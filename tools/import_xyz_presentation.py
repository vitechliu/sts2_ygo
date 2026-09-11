"""从只读 MDPro3 导出超量演出网格、绑定曲线和粒子参数（需要 PyYAML）。

用法：python tools/import_xyz_presentation.py --source <MDPro3>/Assets
缺失的原始 shader 不能自动翻译；输出保留材质来源与占位标记供审计。
"""
import argparse
import json
import re
import shutil
import struct
from pathlib import Path
from convert_unity_mesh_asset import convert, CHANNEL_PATTERN, require, read_components
import yaml


def documents(path):
    text = path.read_text(encoding='utf-8-sig')
    return {int(m[1]): next(iter(yaml.safe_load(m[2]).values())) for m in re.finditer(
        r'^--- !u!\d+ &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)', text, re.M | re.S)}


def mapping(value):
    return {k: v for item in value for k, v in item.items()} if isinstance(value, list) else value


def keys(curve):
    return [[k['time'], k['value'], k['inSlope'], k['outSlope']]
            for k in curve.get('m_Curve', [])]


def clean(value):
    if isinstance(value, dict):
        return {k: clean(v) for k, v in value.items() if k != 'serializedVersion'}
    if isinstance(value, list):
        return [clean(v) for v in value]
    # Unity 的阶跃切线为 Infinity；用字符串保持合法 JSON。
    if isinstance(value, float) and abs(value) == float('inf'):
        return 'step'
    return value


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', required=True, type=Path)
    parser.add_argument('--output', type=Path, default=Path('VYgo/scenes/summon/xyz'))
    args = parser.parse_args()
    source = args.source.resolve()
    root = source / 'AssetBundles/Duel/Timeline/Summon/SummonXYZ'
    target = args.output / 'assets'
    target.mkdir(parents=True, exist_ok=True)
    guid_paths = {}
    for meta in source.rglob('*.meta'):
        match = re.search(r'^guid: (\w+)', meta.read_text(encoding='utf-8-sig'), re.M)
        if match:
            guid_paths[match[1]] = meta.with_suffix('')
    dependencies = {}
    materials = {}
    colored_meshes = {}

    def asset(ref, mesh=False):
        guid = ref.get('guid')
        if not guid or guid.startswith('0000000000000000'):
            return None
        path = guid_paths[guid]
        name = path.stem + '.obj' if mesh else path.name
        dependencies[name] = path.relative_to(source).as_posix()
        if mesh:
            convert(path, target / name)
            mesh_text = path.read_text(encoding='utf-8-sig')
            channels = [{k:int(v) for k,v in match.groupdict().items()} for match in CHANNEL_PATTERN.finditer(mesh_text)]
            if channels[3]['dimension']:
                count = int(require(r'    m_VertexCount: (\d+)',mesh_text,'顶点数'))
                raw = bytes.fromhex(require(r'    _typelessdata: ([0-9a-fA-F]+)',mesh_text,'顶点数据'))
                stride = len(raw)//count
                def channel(index):
                    c=channels[index]
                    return [read_components(raw,i*stride+c['offset'],c['format'],c['dimension']) for i in range(count)]
                indices = bytes.fromhex(require(r'  m_IndexBuffer: ([0-9a-fA-F]+)',mesh_text,'索引'))
                index_format = int(require(r'  m_IndexFormat: (\d+)',mesh_text,'索引格式'))
                code,size = ('H',2) if index_format==0 else ('I',4)
                indices=list(struct.unpack(f'<{len(indices)//size}{code}',indices))
                colored_meshes[name]=dict(vertices=[[x,y,-z] for x,y,z in channel(0)],
                    normals=[[x,y,-z] for x,y,z in channel(1)],uv=[[u,1-v] for u,v in channel(4)],
                    colors=channel(3),indices=[indices[i+j] for i in range(0,len(indices),3) for j in [0,2,1]])
        elif path.suffix == '.png':
            if not (target / name).exists() or (target / name).read_bytes() != path.read_bytes():
                shutil.copyfile(path, target / name)
        return name

    def material(ref):
        guid = ref.get('guid')
        if not guid:
            return None
        if guid in materials:
            return guid
        path = guid_paths[guid]
        mat = documents(path).get(ref['fileID'])
        if mat is None or 'm_SavedProperties' not in mat:
            raise ValueError(f'材质缺失: {path}, {ref}')
        props = mat['m_SavedProperties']
        if mat['m_Name'] in ('lambert1', 'ParticlesUnlit', 'postXYZcardAdd') or mat['m_Name'].startswith('DummyCard'):
            return None
        shader = guid_paths.get(mat['m_Shader'].get('guid'))
        textures = {}
        for key, value in mapping(props['m_TexEnvs']).items():
            if value['m_Texture'].get('guid'):
                textures[key] = asset(value['m_Texture'])
        materials[guid] = dict(name=mat['m_Name'], source=path.relative_to(source).as_posix(),
            shader=shader.relative_to(source).as_posix() if shader else '缺失:'+str(mat['m_Shader']),
            shaderPlaceholder=not shader or 'DummyShaderTextExporter' in shader.read_text(encoding='utf-8-sig', errors='replace'),
            textures=textures, floats=mapping(props['m_Floats']), colors=mapping(props['m_Colors']))
        return guid

    stages = {}
    for name, prefab_rel, timeline_rel in [
        ('galaxy', 'SummonXYZGalaxy01/SummonXYZGalaxy01', 'SummonXYZGalaxy01/SummonXYZGalaxy01TL'),
        ('hole', 'SummonXYZMain/SummonXYZMain02', 'SummonXYZMain/SummonXYZMain02TL'),
        ('explosion', 'SummonXYZExplosion01/SummonXYZExplosion01', 'SummonXYZExplosion01/SummonXYZExplosion01TL'),
        ('post', 'SummonXYZPostXYZ/SummonXYZPostXYZ', 'SummonXYZPostXYZ/SummonXYZPostXYZTL'),
        *[(f'trail{i}', f'SummonXYZTrailIn/XYZTrailIn0{i}', f'SummonXYZTrailIn/XYZTrailIn0{i}TL') for i in range(1,4)],
    ]:
        prefab = documents(root / (prefab_rel + '.prefab'))
        timeline = documents(root / (timeline_rel + '.playable'))
        transforms = {k: o for k, o in prefab.items() if 'm_LocalPosition' in o}
        game_transforms = {o['m_GameObject']['fileID']: k for k, o in transforms.items()}

        def node_path(transform_id):
            t = transforms[transform_id]
            parent = t['m_Father']['fileID']
            if parent == 0:
                return ''
            parent_path = node_path(parent)
            return (parent_path + '/' if parent_path else '') + prefab[t['m_GameObject']['fileID']]['m_Name'].strip()

        def bound_path(object_id):
            obj = prefab[object_id]
            return node_path(game_transforms[object_id if object_id in game_transforms else obj['m_GameObject']['fileID']])

        bindings = {}
        exposed = {}
        for obj in prefab.values():
            for bind in obj.get('m_SceneBindings', []):
                bindings[bind['key']['fileID']] = bound_path(bind['value']['fileID'])
            for pair in obj.get('m_ExposedReferences', {}).get('m_References', []):
                for key, value in pair.items():
                    object_id = value['fileID']
                    game_id = prefab.get(object_id, {}).get('m_GameObject', {}).get('fileID')
                    if object_id in game_transforms or game_id in game_transforms:
                        exposed[str(key)] = bound_path(value['fileID'])
        nodes = []
        for k, trans in transforms.items():
            game_id = trans['m_GameObject']['fileID']
            components = [o for o in prefab.values() if o.get('m_GameObject', {}).get('fileID') == game_id]
            render = next((o for o in components if 'm_Materials' in o), None)
            particle = next((o for o in components if 'InitialModule' in o), None)
            node = dict(path=node_path(k), position=trans['m_LocalPosition'],
                        rotation=trans['m_LocalRotation'], scale=trans['m_LocalScale'],
                        active=bool(prefab[game_id]['m_IsActive']))
            if render:
                node['material'] = material(render['m_Materials'][0])
                mesh = render.get('m_Mesh') or next((o['m_Mesh'] for o in components if 'm_Mesh' in o), {})
                if node['material'] and mesh.get('guid'):
                    node['mesh'] = asset(mesh, True)
            if particle:
                node['particle'] = {key: particle[key] for key in ['lengthInSec', 'looping', 'moveWithTransform',
                    'startDelay', 'InitialModule', 'EmissionModule', 'ShapeModule', 'SizeModule', 'ColorModule',
                    'RotationModule', 'VelocityModule', 'UVModule']}
                node['renderMode'] = render['m_RenderMode']
                node['renderAlignment'] = render.get('m_RenderAlignment', 0)
                node['pivot'] = render.get('m_Pivot', {'x': 0, 'y': 0, 'z': 0})
            nodes.append(node)
        tracks = []
        for track_id, track in timeline.items():
            if 'm_Clips' not in track:
                continue
            clips = track['m_Clips']
            if track.get('m_InfiniteClip', {}).get('fileID'):
                clips = [dict(m_Start=0, m_Duration=10, m_ClipIn=0, m_TimeScale=1,
                    m_Asset=track['m_InfiniteClip'], m_DisplayName='Infinite')]
            for clip in clips:
                obj = timeline[clip['m_Asset']['fileID']]
                entry = dict(start=clip['m_Start'], duration=clip['m_Duration'],
                             clipIn=clip['m_ClipIn'], speed=clip['m_TimeScale'])
                if 'sourceGameObject' in obj:
                    reference = str(obj['sourceGameObject']['exposedName'])
                    if reference not in exposed:
                        # 少数源 Prefab 仍保留旧 Timeline 的暴露名；显示名必须唯一匹配。
                        candidates = [n['path'] for n in nodes if n['path'].split('/')[-1] == clip['m_DisplayName'].strip()]
                        if len(candidates) != 1:
                            if name == 'hole':
                                # Main 中的嵌套 Galaxy / Trail 由独立阶段加载；这里只导出直接黑洞节点。
                                continue
                            raise ValueError(f'{name}: 无法解析 {reference}: {clip["m_DisplayName"]}')
                        exposed[reference] = candidates[0]
                    entry.update(target=exposed[reference], kind='active')
                elif 'm_Clip' in obj or 'm_EditorCurves' in obj:
                    anim = timeline[obj['m_Clip']['fileID']] if 'm_Clip' in obj else obj
                    curves = anim.get('m_EditorCurves') or anim.get('m_FloatCurves') or []
                    entry.update(target=bindings[track_id], kind='animation', curves=[dict(
                        path=c['path'] or '', attribute=c['attribute'], keys=keys(c['curve'])) for c in curves])
                    for curve in anim.get('m_RotationCurves', []):
                        for axis in 'xyzw':
                            entry['curves'].append(dict(path=curve['path'] or '', attribute='m_LocalRotation.'+axis,
                                keys=[[k['time'], k['value'][axis], k['inSlope'][axis], k['outSlope'][axis]] for k in curve['curve']['m_Curve']]))
                elif track_id in bindings:
                    entry.update(target=bindings[track_id], kind='active')
                else:
                    continue
                tracks.append(entry)
        stages[name] = dict(source=prefab_rel, nodes=nodes, tracks=tracks)
    data = clean(dict(stages=stages, materials=materials, dependencies=dependencies, coloredMeshes=colored_meshes))
    (args.output / 'xyz_source.json').write_text(json.dumps(data, ensure_ascii=False, separators=(',', ':'))+'\n', encoding='utf-8')
    print(f'已导出 {len(stages)} 个阶段、{len(materials)} 个材质、{len(dependencies)} 个资源。')


if __name__ == '__main__':
    main()
