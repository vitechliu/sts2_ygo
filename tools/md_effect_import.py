"""只读迁移完整的 74 个独立包；输出限定当前仓库的预览资源。"""
import csv
import hashlib
import json
import math
from pathlib import Path
from md_shader_translate import generate, programs

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'VYgo/scenes/vfx/master_duel'
EVIDENCE=ROOT/'tools/master_duel_effects/source'
MODULES={'InitialModule','EmissionModule','ColorModule','SizeModule','ShapeModule','UVModule','ClampVelocityModule',
         'RotationModule','CustomDataModule','NoiseModule','VelocityModule','SubModule','TrailModule'}

def dump(v):
    def safe(x):
        if isinstance(x,float) and not math.isfinite(x):return 'Infinity' if x>0 else '-Infinity' if x<0 else 'NaN'
        if isinstance(x,dict):return {k:safe(a) for k,a in x.items()}
        if isinstance(x,list):return [safe(a) for a in x]
        return x
    return json.dumps(safe(v),ensure_ascii=False,separators=(',',':'),allow_nan=False)
def write(p,s):
    p.parent.mkdir(parents=True,exist_ok=True); p.write_text(s,encoding='utf-8',newline='\n')
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def rgba(v): return [v[k] for k in 'rgba']
def linear(v): return v/12.92 if v<=0.04045 else ((v+0.055)/1.055)**2.4

class Pack:
    def __init__(self,path):
        self.path=path; self.used={'effect.json'};self.objects={};self.external={};self.geometry={}
        self.effect=json.loads((path/'effect.json').read_text('utf-8'))
        for file in sorted((path/'bundles').glob('*/index.json')):
            self.used.add(file.relative_to(path).as_posix());index=json.loads(file.read_text('utf-8'))
            for sf in index['serialized_files']:
                for ex in sf['externals']:
                    self.external[(sf['name'],ex['file_id'])]=ex['path'].replace('\\','/').split('/')[-1]
            for obj in index['objects']:
                self.objects[(obj['serialized_file'],obj['path_id'])]=(obj,[file.parent/a for a in obj['artifacts']])

    def key(self,sf,ptr):
        return (self.external[(sf,ptr['m_FileID'])] if ptr['m_FileID'] else sf,ptr['m_PathID'])

    def json(self,file):
        self.used.add(file.relative_to(self.path).as_posix());return json.loads(file.read_text('utf-8'))

    def mesh(self,sf,ptr):
        if ptr['m_PathID']==0: return None
        key=self.key(sf,ptr); tag=f'{key[0]}:{key[1]}'
        if tag in self.geometry:return tag
        if key in self.objects:
            obj,files=self.objects[key];g=next(f for f in files if f.name.endswith('.geometry.json'))
            self.geometry[tag]=self.json(g)
        else:
            assert key[1] in (10202,10207,10209,10210),('未知内建网格',key)
            self.geometry[tag]={'builtin':key[1]}
        return tag

    def texture(self,binding):
        texture=binding.get('texture')
        if not texture:return None,None
        png=next((f for f in texture.get('files',[]) if f.endswith('.png')),None)
        if not png:return None,None
        file=self.path/png; self.used.add(png)
        name=sha(file)[:20]+'.png';dest=ROOT/'VYgo/images/vfx/master_duel'/name
        dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(file.read_bytes())
        metadata=next((f for f in texture['files'] if f.endswith('.json')),None)
        data=self.json(self.path/metadata)['data'] if metadata else {}
        return 'res://'+dest.relative_to(ROOT).as_posix(),data

    def material(self,mat,key):
        floats=dict(mat['properties']['m_Floats']); colors=dict(mat['properties']['m_Colors'])
        raw=self.json(self.path/mat['files'][0])['data']
        folder=self.path/'shaders'/mat['shader_profile']; keywords=set(raw['m_ValidKeywords'])
        binary=folder/'platform4_block0.bin';self.used.add(binary.relative_to(self.path).as_posix())
        (EVIDENCE/'shaders'/mat['shader_profile']).mkdir(parents=True,exist_ok=True)
        (EVIDENCE/'shaders'/mat['shader_profile']/binary.name).write_bytes(binary.read_bytes())
        state,p,selected=programs(folder,keywords);rs=p['m_State']
        def setting(d):return int(floats.get(d['name'],d['val']))
        src,dst=setting(rs['rtBlend0']['srcBlend']),setting(rs['rtBlend0']['destBlend'])
        blend={(5,10):'blend_mix',(5,1):'blend_add',(1,0):'blend_mix'}[(src,dst)]
        opaque=(src,dst)==(1,0)
        code,decl,files=generate(folder,keywords,blend,setting(rs['culling']),setting(rs['zWrite']),setting(rs['zTest']))
        if opaque:code=code.replace('ALPHA=o0.a;','// 源 Pass 为不透明写入。')
        bindings={b['property']:b for b in mat['texture_bindings']}
        texenv=dict(mat['properties']['m_TexEnvs'])
        props={p['m_Name']:p for p in state['m_PropInfo']['m_Props']}
        textures={};uniforms=[]
        for name,typ in sorted(decl.items()):
            if typ=='sampler2D':
                if name=='_CameraDepthTexture':continue
                path,metadata=self.texture(bindings.get(name,{}))
                if path is None:
                    default=props.get(name,{}).get('m_DefTexture',{}).get('m_DefaultName','white')
                    default={'gray':'grey','bump':'normal'}.get(default,default)
                    default=default if default in ('white','black','grey','normal') else 'white'
                    path=f'res://VYgo/scenes/vfx/master_duel/default_{default}.tres'
                if metadata:
                    settings=metadata.get('m_TextureSettings',{});wrap=settings.get('m_WrapU',0)
                    assert wrap in (0,1),('未知纹理 Wrap',wrap)
                    if wrap==1:code=code.replace(f'p{name} : source_color, filter_linear_mipmap, repeat_enable',f'p{name} : source_color, filter_linear_mipmap, repeat_disable')
                textures[name]=path;continue
            default=props.get(name,{})
            if name in colors:
                value=rgba(colors[name])
                if default.get('m_Type')==0:value=[linear(v) for v in value[:3]]+[value[3]]
            elif name in floats:value=[floats[name]]
            elif name.endswith('_ST') and name[:-3] in texenv:
                env=texenv[name[:-3]];value=[env['m_Scale']['x'],env['m_Scale']['y'],env['m_Offset']['x'],env['m_Offset']['y']]
            elif default:value=[default[f'm_DefValue[{i}]'] for i in range(4)]
            else:raise ValueError('没有来源的材质参数：'+name)
            count=1 if typ=='float' else int(typ[-1]);value=(value+[0.0]*4)[:count]
            literal=str(value[0]) if count==1 else f'Vector{count}('+','.join(map(str,value))+')'
            uniforms.append(f'shader_parameter/p{name} = {literal}')
        shader_hash=hashlib.sha256(code.encode()).hexdigest()[:12]
        shader_path=f'VYgo/shaders/master_duel/generated/{mat["shader_profile"]}_{shader_hash}.gdshader'
        write(ROOT/shader_path,code)
        resource='[gd_resource type="ShaderMaterial" load_steps='+str(2+len(textures))+' format=3]\n\n'
        resource+=f'[ext_resource type="Shader" path="res://{shader_path}" id="s"]\n'
        for i,(name,path) in enumerate(textures.items()):resource+=f'[ext_resource type="Texture2D" path="{path}" id="t{i}"]\n'
        resource+='\n[resource]\nshader = ExtResource("s")\n'
        for i,name in enumerate(textures):resource+=f'shader_parameter/p{name} = ExtResource("t{i}")\n'
        resource+='\n'.join(uniforms)+'\n'
        material_path=f'{key}/material_{abs(mat["path_id"])}.tres';write(OUT/material_path,resource)
        for file in folder.iterdir():
            if file.name.endswith(('.json','.asm.txt')):
                self.used.add(file.relative_to(self.path).as_posix());write(EVIDENCE/'shaders'/mat['shader_profile']/file.name,file.read_text('utf-8'))
        queue=raw['m_CustomRenderQueue']
        if queue<0:
            tags=dict(state['m_SubShaders'][0]['m_Tags']['tags'])
            queue={'Geometry':2000,'Transparent':3000}.get(tags.get('QUEUE'),2000)
        return material_path,{'shader':mat['shader_profile'],'programs':files,'godot':shader_path,'blend':[src,dst],'queue':queue,
                              'cull':setting(rs['culling']),'zwrite':setting(rs['zWrite']),'ztest':setting(rs['zTest'])}

def import_all(source):
    rows=list(csv.DictReader((source/'catalog.csv').open(encoding='utf-8-sig')));manifest=[];catalog=[]
    for name,c in [('white',[1,1,1,1]),('black',[0,0,0,1]),('grey',[.5,.5,.5,1]),('normal',[.5,.5,1,1])]:
        write(OUT/f'default_{name}.tres','[gd_resource type="GradientTexture2D" load_steps=2 format=3]\n\n[sub_resource type="Gradient" id="g"]\ncolors = PackedColorArray('+','.join(map(str,c+c))+')\n\n[resource]\ngradient = SubResource("g")\nwidth = 1\nheight = 1\n')
    for row in rows:
        if row['入选']!='True':continue
        pack=Pack(source/row['目录']);e=pack.effect
        key={'b9f86e1a':'hit_00','20f13fa0':'hit_03'}.get(e['hash'],'md_'+e['hash'])
        mats=[];mat_index={};shader_records=[]
        for i,mat in enumerate(e['materials']):
            path,record=pack.material(mat,key);mats.append(path);shader_records.append(record)
            mat_index[(mat['serialized_file'],mat['path_id'])]=i
        objects={}
        for file in (pack.path/'bundles'/e['hash']/'GameObject').glob('*.json'):
            obj=pack.json(file);objects[str(obj['path_id'])]={'name':obj['data']['m_Name'],'active':obj['data']['m_IsActive']}
        renderers={(r['serialized_file'],r['data']['m_GameObject']['m_PathID']):r for r in e['renderers'] if r['type']=='ParticleSystemRenderer'}
        emitters=[];notes=set();extras=[];needs_motion=False
        for ps in e['particles']:
            data=pack.json(pack.path/ps['source'])['data'];sf=ps['serialized_file']
            assert set(ps['enabled_modules'])<=MODULES,ps['enabled_modules']
            assert data['simulationSpeed']==1 and data['ringBufferMode']==0
            renderer=renderers[(sf,data['m_GameObject']['m_PathID'])]['data']
            materials=[mat_index[pack.key(sf,m)] if m['m_PathID'] else -1 for m in renderer['m_Materials']]
            meshes=[pack.mesh(sf,renderer[n]) for n in ['m_Mesh','m_Mesh1','m_Mesh2','m_Mesh3'] if n in renderer]
            if 'NoiseModule' in ps['enabled_modules']:notes.add('粒子噪声采用同参数 Perlin 重建，Unity 原生采样差异待对照')
            if 'ClampVelocityModule' in ps['enabled_modules']:notes.add('限速与阻尼采用固定步长积分，原生积分差异待对照')
            if renderer['m_RenderMode']==1:notes.add('拉伸粒子的精确锚点待原效果对照')
            if 'EmissionModule' in ps['enabled_modules'] and data['EmissionModule']['rateOverDistance']['scalar']>0:
                needs_motion=True;notes.add('包含按移动距离发射的粒子，可切换移动演示；演示路径不是源动画')
            emitters.append({'id':ps['path_id'],'name':ps['name'],'data':{k:v for k,v in data.items() if not k.endswith('Module') or k in ps['enabled_modules']},
                             'renderer':renderer,'materials':materials,'meshes':meshes})
        for r in e['renderers']:
            if r['type']=='ParticleSystemRenderer':continue
            sf=r['serialized_file'];data=r['data'];extra={'type':r['type'],'data':data,'materials':[mat_index[pack.key(sf,m)] for m in data['m_Materials']]}
            if r['type']=='SpriteRenderer':
                obj,files=pack.objects[pack.key(sf,data['m_Sprite'])]
                extra['sprite']=pack.json(next(f for f in files if f.suffix=='.json'))['data']
            else:notes.add('TrailRenderer 需要节点移动；可切换移动演示，演示路径不是源动画')
            extras.append(extra)
        if any('builtin' in g for g in pack.geometry.values()):notes.add('Unity 内建网格以对应 Godot 几何体重建，精确拓扑待对照')
        normalized={'name':e['name'],'hash':e['hash'],'objects':objects,'transforms':e['transforms'],'emitters':emitters,
                    'geometry':pack.geometry,'extras':extras,'notes':sorted(notes),'material_queues':[s['queue'] for s in shader_records]}
        resource=f'[gd_resource type="Resource" load_steps={2+len(mats)} format=3]\n\n[ext_resource type="Script" path="res://Core/Effects/MasterDuel/MdSourceData.cs" id="s"]\n'
        for i,path in enumerate(mats):resource+=f'[ext_resource type="ShaderMaterial" path="res://VYgo/scenes/vfx/master_duel/{path}" id="m{i}"]\n'
        resource+='\n[resource]\nscript = ExtResource("s")\nDataJson = '+dump(dump(normalized))+'\nMaterials = Array[ShaderMaterial](['+', '.join(f'ExtResource("m{i}")' for i in range(len(mats)))+'])\n'
        write(OUT/key/'source.tres',resource)
        write(OUT/f'{key}_3d.tscn',f'[gd_scene load_steps=3 format=3]\n\n[ext_resource type="Script" path="res://Core/Effects/MasterDuel/NMdEffect3D.cs" id="s"]\n[ext_resource type="Resource" path="res://VYgo/scenes/vfx/master_duel/{key}/source.tres" id="d"]\n\n[node name="{key}" type="Node3D"]\nscript = ExtResource("s")\nSource = ExtResource("d")\n')
        write(OUT/f'{key}.tscn',f'[gd_scene load_steps=3 format=3]\n\n[ext_resource type="Script" path="res://Core/Effects/MasterDuel/NMdEffect2D.cs" id="s"]\n[ext_resource type="PackedScene" path="res://VYgo/scenes/vfx/master_duel/{key}_3d.tscn" id="d"]\n\n[node name="{key}" type="Node2D"]\nscript = ExtResource("s")\nEffectScene = ExtResource("d")\n')
        write(EVIDENCE/key/'effect.json',json.dumps(e,ensure_ascii=False,indent=2)+'\n')
        catalog.append({'id':key,'name':e['name'],'title':e['name'].split('/')[-1],'category':row['类别'],'particles':len(emitters),
                        'loop':bool(e['looping_particle_count']),'usage':'仅预览，尚未接入游戏','notes':sorted(notes),'trails':needs_motion or any(v['type']=='TrailRenderer' for v in extras)})
        manifest.append({'id':key,'source_pack':row['目录'],'shaders':shader_records,'files':[{'path':f,'sha256':sha(pack.path/f)} for f in sorted(pack.used)]})
        print('已转换',key,len(emitters),'个粒子系统')
    write(OUT/'catalog.tres','[gd_resource type="Resource" load_steps=2 format=3]\n\n[ext_resource type="Script" path="res://Core/Effects/MasterDuel/MdSourceData.cs" id="s"]\n\n[resource]\nscript = ExtResource("s")\nDataJson = '+dump(dump(catalog))+'\n')
    write(EVIDENCE.parent/'manifest.json',json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
    write(EVIDENCE.parent/'pending_controlled.json',json.dumps([r for r in rows if r['入选']!='True'],ensure_ascii=False,indent=2)+'\n')
    write(EVIDENCE.parent/'.gdignore','')
    # 仅清理本导入器拥有的旧散列 Shader，避免重复生成后留下过时变体。
    live_shaders={s['godot'] for p in manifest for s in p['shaders']}
    generated=(ROOT/'VYgo/shaders/master_duel/generated').resolve()
    for shader in generated.glob('*.gdshader'):
        if shader.relative_to(ROOT).as_posix() in live_shaders:continue
        assert shader.resolve().is_relative_to(generated)
        shader.unlink()
        shader.with_suffix('.gdshader.uid').unlink(missing_ok=True)
    assert len(catalog)==74 and sum(c['particles'] for c in catalog)==507
    print('完成 74 个独立包，507 个粒子系统；62 个控制入口只登记，不虚构控制逻辑。')
