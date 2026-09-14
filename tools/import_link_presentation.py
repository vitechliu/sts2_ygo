"""只读审计连接依赖；显式源路径，保留嵌套 Prefab 覆盖、轨道偏移与曲线。"""
import argparse,json,re,shutil,hashlib,struct
from pathlib import Path
import yaml
from import_xyz_presentation import clean,mapping,keys
from convert_unity_mesh_asset import convert,CHANNEL_PATTERN,require,read_components


def documents(path):
    text=path.read_text(encoding='utf-8-sig')
    return {int(m[1]):next(iter(yaml.load(m[2],Loader=yaml.CSafeLoader).values())) for m in re.finditer(r'^--- !u!\d+ &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)',text,re.M|re.S)}


def main():
    ap=argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--source',type=Path,required=True)
    ap.add_argument('--output',type=Path,default=Path('.context/link-source'))
    ap.add_argument('--runtime-output',type=Path,default=Path('VYgo/scenes/summon/link/link_material_source.json'))
    args=ap.parse_args()
    source=args.source.resolve();root=source/'AssetBundles/Duel/Timeline/Summon/SummonLink';out=args.output.resolve()
    if out.is_relative_to(source) or args.runtime_output.resolve().is_relative_to(source):raise ValueError('输出目录不得位于只读源工程内。')
    assets=out/'assets';assets.mkdir(parents=True,exist_ok=True)
    paths={}
    for p in source.rglob('*.meta'):
        m=re.search(r'^guid: (\w+)',p.read_text(encoding='utf-8-sig'),re.M)
        if m:paths[m[1]]=p.with_suffix('')
    deps={};mats={};colored_meshes={};warnings=[];next_id=-1
    def record(p):
        deps[p.relative_to(source).as_posix()]=hashlib.sha256(p.read_bytes()).hexdigest()
    def flatten(p):
        nonlocal next_id
        record(p);d=documents(p)
        for iid,instance in list(d.items()):
            if 'm_SourcePrefab' not in instance:continue
            sub=flatten(paths[instance['m_SourcePrefab']['guid']]);remap={}
            for k in sub:remap[k]=next_id;next_id-=1
            for k,o in d.items():
                if o.get('m_PrefabInstance',{}).get('fileID')==iid:remap[o['m_CorrespondingSourceObject']['fileID']]=k
            for mod in instance['m_Modification']['m_Modifications']:
                target=sub.get(mod['target']['fileID']);parts=mod['propertyPath'].split('.')
                if target is None:continue
                for bit in parts[:-1]:target=target.get(bit,{})
                if parts[-1] not in target:continue
                old=target[parts[-1]]
                if isinstance(old,dict):target[parts[-1]]=mod['objectReference']
                else:
                    try:target[parts[-1]]=type(old)(mod['value'])
                    except (ValueError,TypeError):target[parts[-1]]=mod['value']
            def refs(o):
                if isinstance(o,dict):
                    if 'fileID' in o and not o.get('guid') and o['fileID'] in remap:o['fileID']=remap[o['fileID']]
                    else:
                        for v in o.values():refs(v)
                elif isinstance(o,list):
                    for v in o:refs(v)
            for k,o in sub.items():
                refs(o)
                if 'm_LocalPosition' in o and o['m_Father']['fileID']==0:o['m_Father']=instance['m_Modification']['m_TransformParent']
                d[remap[k]]=o
        return d
    def asset(ref,mesh=False):
        guid=ref.get('guid');p=paths.get(guid)
        if p is None:return None
        record(p);name=guid[:8]+'_'+(p.stem+'.obj' if mesh else p.name)
        if mesh:convert(p,assets/name)
        else:shutil.copyfile(p,assets/name)
        if mesh and name not in colored_meshes:
            text=p.read_text(encoding='utf-8-sig');channels=[{k:int(v) for k,v in m.groupdict().items()} for m in CHANNEL_PATTERN.finditer(text)]
            if channels[3]['dimension']:
                count=int(require(r'    m_VertexCount: (\d+)',text,'顶点数'));raw=bytes.fromhex(require(r'    _typelessdata: ([0-9a-fA-F]+)',text,'顶点数据'));stride=len(raw)//count
                def channel(i):
                    c=channels[i];return [read_components(raw,j*stride+c['offset'],c['format'],c['dimension']) for j in range(count)]
                raw_indices=bytes.fromhex(require(r'  m_IndexBuffer: ([0-9a-fA-F]+)',text,'索引'));fmt=int(require(r'  m_IndexFormat: (\d+)',text,'索引格式'));code,size=('H',2) if fmt==0 else ('I',4);indices=struct.unpack(f'<{len(raw_indices)//size}{code}',raw_indices)
                colored_meshes[name]=dict(vertices=[[v[0],v[1],-v[2]] for v in channel(0)],normals=[[v[0],v[1],-v[2]] for v in channel(1)],uv=[[v[0],1-v[1]] for v in channel(4)],colors=channel(3),indices=[indices[i+j] for i in range(0,len(indices),3) for j in [0,2,1]])
        return name
    def material(ref):
        guid=ref.get('guid');p=paths.get(guid)
        if p is None:return None
        if guid in mats:return guid
        record(p);m=documents(p)[ref['fileID']];props=m['m_SavedProperties'];shader=paths.get(m['m_Shader'].get('guid'))
        if shader:record(shader)
        textures={k:dict(file=asset(v['m_Texture']),scale=v['m_Scale'],offset=v['m_Offset']) for k,v in mapping(props['m_TexEnvs']).items() if v['m_Texture'].get('guid')}
        placeholder=not shader or 'DummyShaderTextExporter' in shader.read_text(encoding='utf-8-sig',errors='replace')
        mats[guid]=dict(name=m['m_Name'],source=p.relative_to(source).as_posix(),shader=shader.relative_to(source).as_posix() if shader else '缺失',reconstructed=placeholder,textures=textures,floats=mapping(props['m_Floats']),colors=mapping(props['m_Colors']),renderQueue=m.get('m_CustomRenderQueue',-1))
        return guid
    stages={}
    for p in sorted(root.rglob('*.prefab')):
        name=p.stem
        d=flatten(p);trans={k:o for k,o in d.items() if 'm_LocalPosition' in o};gt={o['m_GameObject']['fileID']:k for k,o in trans.items()}
        def path(k):
            o=trans[k];parent=o['m_Father']['fileID']
            if not parent:return ''
            return '/'.join(filter(None,[path(parent),d[o['m_GameObject']['fileID']]['m_Name'].strip()]))
        def bound(k):return path(gt[k if k in gt else d[k]['m_GameObject']['fileID']])
        nodes=[];directors={}
        for k,t in trans.items():
            gid=t['m_GameObject']['fileID'];cs=[o for o in d.values() if o.get('m_GameObject',{}).get('fileID')==gid];render=next((o for o in cs if o.get('m_Materials')),None);particle=next((o for o in cs if 'InitialModule' in o),None)
            node=dict(path=path(k),position=t['m_LocalPosition'],rotation=t['m_LocalRotation'],scale=t['m_LocalScale'],active=bool(d[gid]['m_IsActive']))
            if render:
                node['material']=material(render['m_Materials'][0]);mesh=render.get('m_Mesh') or next((o['m_Mesh'] for o in cs if 'm_Mesh' in o),{})
                node['mesh']=asset(mesh,True) if mesh.get('guid') else None
            if particle:
                node['particle']={k:particle[k] for k in ['lengthInSec','looping','moveWithTransform','startDelay','InitialModule','EmissionModule','ShapeModule','SizeModule','ColorModule','RotationModule','VelocityModule','UVModule']}
                node.update(renderMode=render['m_RenderMode'],renderAlignment=render.get('m_RenderAlignment',0),pivot=render.get('m_Pivot',dict(x=0,y=0,z=0)))
            nodes.append(node)
            director=next((o for o in cs if 'm_PlayableAsset' in o),None)
            if director:directors[path(k)]=director
        tracks=[];events=[];visited=set()
        def timeline(dp,start,duration):
            if dp in visited:return
            visited.add(dp);director=directors[dp];tp=paths[director['m_PlayableAsset']['guid']];record(tp);td=documents(tp)
            bindings={b['key']['fileID']:bound(b['value']['fileID']) for b in director.get('m_SceneBindings',[]) if b['value']['fileID'] in d}
            exposed={str(k):bound(v['fileID']) for pair in director.get('m_ExposedReferences',{}).get('m_References',[]) for k,v in pair.items() if v['fileID'] in d}
            for tid,tr in td.items():
                if 'm_Clips' not in tr or tr.get('m_Muted'):continue
                infinite=bool(tr.get('m_InfiniteClip',{}).get('fileID'));clips=tr['m_Clips']
                if infinite:clips=[dict(m_Start=0,m_Duration=duration,m_ClipIn=tr.get('m_InfiniteClipTimeOffset',0),m_TimeScale=1,m_Asset=tr['m_InfiniteClip'],m_DisplayName='Infinite')]
                for c in clips:
                    obj=td[c['m_Asset']['fileID']];entry=dict(start=start+c['m_Start'],duration=c['m_Duration'],clipIn=c['m_ClipIn'],speed=c['m_TimeScale'],track=tr.get('m_Name'),timeline=tp.relative_to(root).as_posix())
                    if 'sourceGameObject' in obj:
                        target=exposed.get(str(obj['sourceGameObject']['exposedName']))
                        if target is None:raise ValueError(f'未绑定控制轨 {tp} {c["m_DisplayName"]}')
                        entry.update(kind='active',target=target);tracks.append(entry)
                        if target in directors:timeline(target,entry['start'],entry['duration'])
                    elif 'm_Clip' in obj or 'm_EditorCurves' in obj:
                        if tid not in bindings:raise ValueError(f'未绑定动画 {tp} {tid}')
                        anim=td[obj['m_Clip']['fileID']] if 'm_Clip' in obj else obj
                        curves=anim.get('m_EditorCurves') or anim.get('m_FloatCurves') or []
                        entry.update(kind='animation',target=bindings[tid],curves=[dict(path=c['path'] or '',attribute=c['attribute'],keys=keys(c['curve'])) for c in curves],offsetPosition=tr.get('m_InfiniteClipOffsetPosition' if infinite else 'm_Position',dict(x=0,y=0,z=0)),offsetEuler=tr.get('m_InfiniteClipOffsetEulerAngles' if infinite else 'm_EulerAngles',dict(x=0,y=0,z=0)),clipPosition=obj.get('m_Position',dict(x=0,y=0,z=0)),clipEuler=obj.get('m_EulerAngles',dict(x=0,y=0,z=0)))
                        for curve in anim.get('m_RotationCurves',[]):
                            for axis in 'xyzw':entry['curves'].append(dict(path=curve['path'] or '',attribute='m_LocalRotation.'+axis,keys=[[k['time'],k['value'][axis],k['inSlope'][axis],k['outSlope'][axis]] for k in curve['curve']['m_Curve']]))
                        tracks.append(entry)
                    elif tid in bindings:entry.update(kind='active',target=bindings[tid]);tracks.append(entry)
                    else:events.append(dict(**entry,label=c['m_DisplayName'],asset=obj))
        if '' in directors:
            timeline('',0,10)
        duration=max((t['start']+t['duration'] for t in tracks if t['duration'] < 9.99),default=0)
        stages[name]=dict(nodes=nodes,tracks=tracks,events=events,duration=duration)
    data=clean(dict(stages=stages,materials=mats,dependencies=deps,coloredMeshes=colored_meshes,warnings=warnings))
    (out/'link_source.json').write_text(json.dumps(data,ensure_ascii=False,separators=(',',':'))+'\n',encoding='utf-8')
    # 正式运行仅需卡面祖先变换与相应曲线；完整粒子、材质、网格依赖保留在审计输出。
    previews={}
    for i in range(1,9):
        stage=stages[f'SummonLinkShowUnitCard{i:02}']
        faces=[n['path'] for n in stage['nodes'] if n['path'].endswith('/CardModel_front')]
        selected={n['path'] for n in stage['nodes'] if any(f==n['path'] or f.startswith(n['path']+'/') for f in faces)}|{''}
        nodes=[{k:v for k,v in n.items() if k in ('path','position','rotation','scale','active')} for n in stage['nodes'] if n['path'] in selected]
        tracks=[]
        for t in stage['tracks']:
            if t['kind']!='animation' or t['target'] not in selected:continue
            curves=[c for c in t['curves'] if c['attribute'].startswith(('localEulerAnglesRaw.','m_LocalRotation.','m_LocalPosition.','m_LocalScale.'))]
            if curves:tracks.append(dict(t,curves=curves,duration=1.3333334))
        previews[str(i)]=dict(nodes=nodes,tracks=tracks,faces=sorted(faces),duration=1.3333334)
    args.runtime_output.parent.mkdir(parents=True,exist_ok=True)
    args.runtime_output.write_text(json.dumps(clean(previews),ensure_ascii=False,separators=(',',':'))+'\n',encoding='utf-8')
    print(f'导出 {len(stages)} 组，{len(mats)} 材质，{len(deps)} 依赖；重建 shader 材质 {sum(m["reconstructed"] for m in mats.values())}。')

if __name__=='__main__':main()
