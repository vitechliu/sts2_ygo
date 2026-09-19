"""将本批 DXBC 前向程序逐指令转为 Godot Shader；未知指令或绑定必须报错。"""
import json
import re
import struct
from pathlib import Path

CHANNELS = 'xyzw'


def merge_variant_parameters(folder, p, stage, variant):
    """读取实际参数 blob 的字节偏移，补全 common 中省略的变体绑定。"""
    prog=p[stage]
    slot=next((a,b) for a,group in enumerate(prog['m_PlayerSubPrograms']) for b,v in enumerate(group) if v is variant)
    blob_id=prog['m_ParameterBlobIndices'][slot[0]][slot[1]]
    binary=(folder/'platform4_block0.bin').read_bytes()
    offset,length,segment=struct.unpack_from('<3I',binary,4+blob_id*12)
    assert segment==0
    data=binary[offset:offset+length];pos=24
    assert struct.unpack_from('<I',data)[0]==202012090
    names={n:i for n,i in p['m_NameIndices']}
    def name_id(name):
        if name not in names:
            names[name]=max(names.values(),default=-1)+1;p['m_NameIndices'].append([name,names[name]])
        return names[name]
    def integer():
        nonlocal pos
        value=struct.unpack_from('<I',data,pos)[0];pos+=4;return value
    def string():
        nonlocal pos
        count=integer();assert count<1024
        name=data[pos:pos+count].decode('ascii');pos=(pos+count+3)&~3;return name
    common=prog['m_CommonParameters']
    blocks={b['m_NameIndex']:b for b in common['m_ConstantBuffers']}
    # 参数尾部首先是绑定数量（很小），CB 名称以非零 ASCII 长度及字符开头。
    while pos+8<=len(data):
        size=struct.unpack_from('<I',data,pos)[0]
        if not (size>0 and pos+4+size<=len(data) and all(32<=c<127 for c in data[pos+4:pos+4+size])):break
        name=string();byte_size=integer();idx=name_id(name)
        block=blocks.get(idx)
        if block is None:
            block={'m_NameIndex':idx,'m_VectorParams':[],'m_MatrixParams':[],'m_Size':byte_size};blocks[idx]=block;common['m_ConstantBuffers'].append(block)
        for _ in range(integer()):
            member=string();typ,rows,dim,matrix,array,index=[integer() for _ in range(6)]
            value={'m_NameIndex':name_id(member),'m_Index':index,'m_Type':typ,'m_ArraySize':array}
            value['m_RowCount' if matrix else 'm_Dim']=rows if matrix else dim
            values=block['m_MatrixParams' if matrix else 'm_VectorParams']
            if not any(v['m_NameIndex']==value['m_NameIndex'] for v in values):values.append(value)
        assert integer()==0, '本批不应有结构体常量参数'
    for _ in range(integer()):
        name=string();kind=integer();index=integer()
        if kind==0:
            sampler,dimension=integer(),integer();assert dimension==4
            value={'m_NameIndex':name_id(name),'m_Index':index,'m_SamplerIndex':sampler,'m_Dim':2}
            if not any(v['m_Index']==index for v in common['m_TextureParams']):common['m_TextureParams'].append(value)
        elif kind==1:
            array=integer();value={'m_NameIndex':name_id(name),'m_Index':index,'m_ArraySize':array}
            if not any(v['m_Index']==index for v in common['m_ConstantBufferBindings']):common['m_ConstantBufferBindings'].append(value)
        elif kind==4:integer() # 内联采样器状态，贴图采样状态由材质贴图元数据保留。
        else:raise ValueError('未知参数绑定种类：'+str(kind))
    assert pos==len(data),(pos,len(data))


def split_args(text):
    result, start, depth = [], 0, 0
    for i, char in enumerate(text):
        depth += char in '(['
        depth -= char in ')]'
        if char == ',' and depth == 0:
            result.append(text[start:i].strip()); start = i + 1
    result.append(text[start:].strip())
    return result


def signature(text, output=False):
    section = text.split('// Output signature:' if output else '// Input signature:')[1]
    section = section.split('\nvs_')[0].split('\nps_')[0]
    if not output: section = section.split('// Output signature:')[0]
    result = []
    for line in section.splitlines():
        match = re.match(r'//\s+(\w+)\s+(\d+)\s+([xyzw]+)\s+(\d+)\s+(\w+)\s+(\w+)', line)
        if match:
            name, index, mask, register, system, fmt = match.groups()
            result.append((name, int(index), mask, int(register), system))
    return result


def programs(folder, keywords):
    state = json.loads((folder/'render-state.json').read_text('utf-8'))
    passes = state['m_SubShaders'][0]['m_Passes']
    blobs = set()
    for p in passes:
        for stage in ['progVertex', 'progFragment']:
            for group in p[stage]['m_PlayerSubPrograms']:
                blobs.update(v['m_BlobIndex'] for v in group)
    mapping = {b:i for i,b in enumerate(sorted(blobs))}
    p = passes[0]
    selections = []
    for stage in ['progVertex', 'progFragment']:
        variants = [v for group in p[stage]['m_PlayerSubPrograms'] for v in group]
        variants = [v for v in variants if not {'PROCEDURAL_INSTANCING_ON','INSTANCING_ON'} &
                    {state['m_KeywordNames'][i] for i in v['m_KeywordIndices']}]
        def rank(v):
            keys = {state['m_KeywordNames'][i] for i in v['m_KeywordIndices']}
            return (len(keys-keywords), -len(keys & keywords))
        selected = min(variants, key=rank)
        merge_variant_parameters(folder,p,stage,selected)
        file = folder/f'platform4_block0_program{mapping[selected["m_BlobIndex"]]}.asm.txt'
        text = file.read_text('utf-8')
        assert ('vs_' if stage == 'progVertex' else 'ps_') in text, file
        selections.append((stage, text, file.name))
    return state, p, selections


class Translator:
    def __init__(self, p, stage, text, declarations):
        self.text, self.stage, self.declarations = text, stage, declarations
        self.names = {index:name for name,index in p['m_NameIndices']}
        self.cb = {}; self.textures = {}
        common = p[stage]['m_CommonParameters']
        binds = {v['m_NameIndex']:v['m_Index'] for v in common['m_ConstantBufferBindings']}
        for block in common['m_ConstantBuffers']:
            bind = binds[block['m_NameIndex']]
            for value in block['m_VectorParams']:
                name = self.names[value['m_NameIndex']]
                index, offset = divmod(value['m_Index'], 16)
                offset //= 4
                assert value['m_ArraySize'] in (0,1), (name,value)
                expression = self.uniform(name, value['m_Dim'])
                for i in range(value['m_Dim']):
                    self.cb.setdefault((bind,index), ['0.0']*4)[offset+i] = expression if value['m_Dim']==1 else expression+'.'+CHANNELS[i]
            for value in block['m_MatrixParams']:
                name = self.names[value['m_NameIndex']]
                expr = {'unity_ObjectToWorld':'md_model','unity_WorldToObject':'md_inverse_model',
                        'unity_MatrixVP':'md_vp','unity_MatrixV':'md_view','unity_MatrixInvVP':'md_inverse_vp',
                        'glstate_matrix_projection':'PROJECTION_MATRIX'}.get(name)
                if not expr: raise ValueError('未映射矩阵：'+name)
                for i in range(value['m_RowCount']):
                    self.cb[(bind,value['m_Index']//16+i)] = [f'{expr}[{i}].{c}' for c in CHANNELS]
        for value in common['m_TextureParams']:
            name = self.names[value['m_NameIndex']]
            assert value['m_Dim']==2, (name, value)
            self.textures[value['m_Index']] = name
            self.declarations[name] = 'sampler2D'

    def uniform(self, name, dim):
        dynamic = {
            '_Time':'vec4(md_time/20.0,md_time,md_time*2.0,md_time*3.0)',
            '_TimeParameters':'vec4(md_time,sin(md_time),cos(md_time),0.0)',
            '_SinTime':'sin(vec4(md_time/8.0,md_time/4.0,md_time/2.0,md_time))',
            '_CosTime':'cos(vec4(md_time/8.0,md_time/4.0,md_time/2.0,md_time))',
            '_GlobalMipBias':'vec2(0.0)', '_WorldSpaceCameraPos':'(INV_VIEW_MATRIX[3].xyz*vec3(1.0,1.0,-1.0))',
            '_ProjectionParams':'vec4(1.0,md_near,md_far,1.0/md_far)',
            '_ZBufferParams':'vec4(md_far/md_near-1.0,1.0,(md_far/md_near-1.0)/md_far,1.0/md_far)',
            'unity_OrthoParams':'vec4(md_camera_size,md_camera_size,0.0,1.0)',
            '_ScreenParams':'vec4(VIEWPORT_SIZE,1.0+1.0/VIEWPORT_SIZE)',
            '_ScaledScreenParams':'vec4(VIEWPORT_SIZE,1.0+1.0/VIEWPORT_SIZE)',
            '_RTHandleScale':'vec4(1.0)', '_CameraDepthTexture_TexelSize':'vec4(1.0/VIEWPORT_SIZE,VIEWPORT_SIZE)',
            'unity_FogColor':'vec4(0.0)', 'unity_FogParams':'vec4(0.0,0.0,0.0,1.0)',
        }
        if name in dynamic: return '('+dynamic[name]+')'
        typ = 'float' if dim==1 else f'vec{dim}'
        previous = self.declarations.setdefault(name, typ)
        assert previous==typ, (name, previous, typ)
        return 'p'+name

    def source(self, text):
        text=text.strip()
        if text.startswith('-'): return '-('+self.source(text[1:])+')'
        if text.startswith('|') and text.endswith('|'): return 'abs('+self.source(text[1:-1])+')'
        if text.startswith('l('):
            values=split_args(text[2:-1]); values=values*4 if len(values)==1 else values
            assert len(values)==4, text
            def literal(v):
                if '#INF' in v: return 'uintBitsToFloat('+('4286578688u' if v.startswith('-') else '2139095040u')+')'
                if '#IND' in v or '#QNAN' in v: return 'uintBitsToFloat(2143289344u)'
                if re.fullmatch(r'-?\d+|0x[0-9a-fA-F]+',v):
                    bits=int(v,0) if v.startswith('0x') else int(v)
                    return '0.0' if bits==0 else f'uintBitsToFloat({bits & 0xffffffff}u)'
                return v if '.' in v or 'e' in v.lower() else v+'.0'
            return 'vec4('+','.join(literal(v) for v in values)+')'
        match=re.fullmatch(r'(cb\d+\[\d+\]|[rvo]\d+)(?:\.([xyzw]+))?',text)
        if not match: raise ValueError('未知源操作数：'+text)
        base, swizzle=match.groups();swizzle=swizzle or 'xyzw'
        swizzle=(swizzle*4)[:4] if len(swizzle)==1 else (swizzle+'wwww')[:4]
        if base.startswith('cb'):
            b,i=map(int,re.findall(r'\d+',base)); values=self.cb.get((b,i))
            if values is None: raise ValueError(f'未映射常量 {base} / {self.stage}')
            base='vec4('+','.join(values)+')'
        return f'({base}).{swizzle}'

    def target(self, text, expression):
        if text=='null': return ''
        base, _, mask=text.partition('.');mask=mask or 'xyzw'
        return f'{base}.{mask} = ({expression}).{mask};'

    def translate(self):
        lines=[]; regs=set(); loop=0; sincos_temporary=0
        for line in self.text.splitlines():
            line=line.strip()
            if not line or line.startswith(('//','dcl_','vs_','ps_')): continue
            op, _, args=line.partition(' '); a=split_args(args); sat=op.endswith('_sat');op=op.removesuffix('_sat')
            for reg in re.findall(r'\b[rvo]\d+\b',line): regs.add(reg)
            if op=='ret': continue
            if op=='loop': lines.append(f'for (int md_loop_{loop}=0; md_loop_{loop}<1024; md_loop_{loop}++) {{');loop+=1;continue
            if op in ('endloop','endif'): lines.append('}');continue
            if op=='else': lines.append('} else {');continue
            if op in ('if_nz','breakc_nz','discard_nz'):
                condition=f'any(notEqual({self.source(a[0])},vec4(0.0)))'
                lines.append(f'if ({condition}) '+('{' if op=='if_nz' else '{ break; }' if op=='breakc_nz' else '{ discard; }'));continue
            if op=='sincos':
                source=self.source(a[2])
                if a[0]!='null' and a[1]!='null' and a[0].split('.')[0] in re.findall(r'\b[rvo]\d+\b',a[2]):
                    # DXBC 同时读取源值；第一条赋值不能改变第二个输出的输入。
                    temporary=f'md_sincos_{sincos_temporary}';sincos_temporary+=1
                    lines.append(f'vec4 {temporary}={source};');source=temporary
                lines += [self.target(a[0],f'sin({source})'),self.target(a[1],f'cos({source})')];continue
            if op.startswith('sample'):
                tex=re.fullmatch(r't(\d+)(?:\.([xyzw]+))?',a[2]);name=self.textures[int(tex[1])];uv=f'({self.source(a[1])}).xy'
                # 原 PNG 为顶部起始；DXBC UV 保持不变，只在采样时翻转 V。
                uv=f'vec2(({uv}).x,1.0-({uv}).y)'
                bias=f',({self.source(a[4])}).x' if len(a)>4 else ''
                expression=f'texture(p{name},{uv}{bias})'
                if name=='_CameraDepthTexture':
                    # 深度纹理使用 Godot 屏幕 UV，不执行图片翻转。
                    expression=f'vec4(texture(p{name},({self.source(a[1])}).xy).r)'
                if tex[2]: expression=f'({expression}).{tex[2]}'
            else:
                s=[self.source(x) for x in a[1:]]
                unary={'mov':'{0}','frc':'fract({0})','rsq':'inversesqrt({0})','sqrt':'sqrt({0})','rcp':'(vec4(1.0)/{0})',
                       'log':'log2({0})','exp':'exp2({0})','round_ni':'floor({0})','round_ne':'roundEven({0})',
                       'deriv_rtx':'dFdx({0})','deriv_rty':'dFdy({0})','itof':'vec4(floatBitsToInt({0}))','utof':'vec4(floatBitsToUint({0}))'}
                binary={'add':'({0}+{1})','mul':'({0}*{1})','div':'({0}/{1})','min':'min({0},{1})','max':'max({0},{1})',
                        'and':'intBitsToFloat(floatBitsToInt({0}) & floatBitsToInt({1}))',
                        'or':'intBitsToFloat(floatBitsToInt({0}) | floatBitsToInt({1}))',
                        'iadd':'intBitsToFloat(floatBitsToInt({0}) + floatBitsToInt({1}))'}
                comparison={'ge':'greaterThanEqual','lt':'lessThan','eq':'equal','ne':'notEqual','ilt':'lessThan'}
                if op in unary: expression=unary[op].format(*s)
                elif op in binary: expression=binary[op].format(*s)
                elif op=='mad': expression=f'({s[0]}*{s[1]}+{s[2]})'
                elif op=='movc': expression=f'mix({s[2]},{s[1]},notEqual({s[0]},vec4(0.0)))'
                elif op in comparison:
                    left,right=(f'floatBitsToInt({v})' for v in s) if op=='ilt' else s
                    expression=f'intBitsToFloat(-ivec4({comparison[op]}({left},{right})))'
                elif op in ('dp2','dp3','dp4'):
                    mask=CHANNELS[:int(op[-1])];expression=f'vec4(dot(({s[0]}).{mask},({s[1]}).{mask}))'
                else: raise ValueError('未实现 DXBC 指令：'+line)
            if sat: expression=f'clamp({expression},vec4(0.0),vec4(1.0))'
            lines.append(self.target(a[0],expression))
        return sorted(regs),lines


def generate(folder, keywords, blend, cull, depth_write, depth_test):
    state,p,selected=programs(folder,keywords)
    declarations={}; stages=[];textures={}
    for stage,text,file in selected:
        t=Translator(p,stage,text,declarations);regs,lines=t.translate();stages.append((stage,text,file,regs,lines));textures.update(t.textures)
    modes=['unshaded',blend, 'depth_draw_always' if depth_write else 'depth_draw_never',
           {0:'cull_disabled',1:'cull_front',2:'cull_back'}[cull]]
    if depth_test in (0,8): modes.append('depth_test_disabled')
    elif depth_test !=4: raise ValueError('尚不支持的深度比较：'+str(depth_test))
    out=['shader_type spatial;', 'render_mode '+', '.join(modes)+';',
         '// 按源 DXBC 前向程序逐指令生成；源文件和散列见迁移清单。',
         'uniform float md_time = 0.0;', 'uniform float md_near = 0.1;', 'uniform float md_far = 300.0;',
         'uniform float md_camera_size = 80.0;', 'const mat4 md_flip = mat4(vec4(1,0,0,0),vec4(0,1,0,0),vec4(0,0,-1,0),vec4(0,0,0,1));']
    for name,typ in sorted(declarations.items()):
        if typ=='sampler2D':
            hints='hint_depth_texture, filter_nearest, repeat_disable' if name=='_CameraDepthTexture' else 'source_color, filter_linear_mipmap, repeat_enable'
            out.append(f'uniform sampler2D p{name} : {hints};')
        else: out.append(f'uniform {typ} p{name};')
    vertex_outputs=signature(stages[0][1],True)
    for name,index,mask,reg,sys in vertex_outputs:
        if sys!='POS': out.append(f'varying vec4 md_vary_{reg};')
    for stage,text,file,regs,lines in stages:
        vertex=stage=='progVertex';out.append('// '+file);out.append('void '+('vertex' if vertex else 'fragment')+'() {')
        out+=['mat4 md_model=md_flip*MODEL_MATRIX*md_flip;', 'mat4 md_inverse_model=inverse(md_model);',
              'mat4 md_vp=PROJECTION_MATRIX*VIEW_MATRIX*md_flip;', 'mat4 md_inverse_vp=inverse(md_vp);',
              'mat4 md_view=VIEW_MATRIX*md_flip;']
        regs=sorted(set(regs) | {f'o{r}' for n,i,m,r,s in signature(text,True)})
        out += [f'vec4 {r}=vec4(0.0);' for r in regs]
        for name,index,mask,reg,sys in signature(text):
            if f'v{reg}' not in regs: continue
            if vertex:
                expression={'POSITION':'vec4(VERTEX*vec3(1,1,-1),1.0)','NORMAL':'vec4(NORMAL*vec3(1,1,-1),0.0)',
                            'TANGENT':'vec4(TANGENT*vec3(1,1,-1),1.0)','COLOR':'COLOR'}.get(name)
                if name=='TEXCOORD': expression=['vec4(UV,UV2)','CUSTOM0','CUSTOM1','CUSTOM2','CUSTOM3'][index]
                if not expression: raise ValueError('未实现顶点语义：'+name)
            else:
                if sys=='POS': expression='FRAGCOORD'
                else:
                    source=next(r for n,i,m,r,s in vertex_outputs if n==name and i==index)
                    expression=f'md_vary_{source}'
            out.append(f'v{reg}={expression};')
        out+=lines
        if vertex:
            for name,index,mask,reg,sys in vertex_outputs:
                out.append(f'{"POSITION" if sys=="POS" else "md_vary_"+str(reg)}=o{reg};')
        else: out+=['ALBEDO=o0.rgb;', 'ALPHA=o0.a;']
        out.append('}')
    return '\n'.join(out)+'\n', declarations, [s[2] for s in stages]
