"""检查 74 项预览资源、引用及可选的只读源文件散列和运行报告。"""
import argparse
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / 'VYgo/scenes/vfx/master_duel'
EVIDENCE = ROOT / 'tools/master_duel_effects'


def resource_data(path):
    line = next(line for line in path.read_text('utf-8').splitlines() if line.startswith('DataJson = '))
    return json.loads(json.loads(line.removeprefix('DataJson = ')))


def validate(source=None, runtime_report=None):
    catalog = resource_data(DATA / 'catalog.tres')
    manifest = json.loads((EVIDENCE / 'manifest.json').read_text('utf-8'))
    ids = {entry['id'] for entry in catalog}
    assert len(catalog) == len(ids) == len(manifest) == 74
    assert ids == {entry['id'] for entry in manifest}
    assert len(json.loads((EVIDENCE / 'pending_controlled.json').read_text('utf-8'))) == 62
    particles = 0
    for entry in catalog:
        data = resource_data(DATA / entry['id'] / 'source.tres')
        assert len(data['emitters']) == entry['particles']
        particles += len(data['emitters'])
        for emitter in data['emitters']:
            assert str(emitter['data']['m_GameObject']['m_PathID']) in data['objects']
            assert all(mesh is None or mesh in data['geometry'] for mesh in emitter['meshes'])
        assert (DATA / (entry['id'] + '.tscn')).is_file()
        assert (DATA / (entry['id'] + '_3d.tscn')).is_file()
    assert particles == 507
    visited = set()
    pending = [DATA / 'catalog.tres'] + list(DATA.glob('*.tscn'))
    while pending:
        path = pending.pop().resolve()
        assert path.is_relative_to(ROOT) and path.is_file(), path
        if path in visited:
            continue
        visited.add(path)
        if path.suffix in ('.tres', '.tscn'):
            pending.extend(ROOT / name for name in re.findall(r'path="res://([^\"]+)"', path.read_text('utf-8')))
    shader_ids = {shader['shader'] for entry in manifest for shader in entry['shaders']}
    assert len(shader_ids) == 41
    for entry in manifest:
        for shader in entry['shaders']:
            assert (ROOT / shader['godot']).resolve() in visited
            for program in shader['programs']:
                assert (EVIDENCE / 'source/shaders' / shader['shader'] / program).is_file()
    if source:
        checked = 0
        for entry in manifest:
            pack = (source / entry['source_pack']).resolve()
            assert pack.is_relative_to(source.resolve())
            for record in entry['files']:
                path = (pack / record['path']).resolve()
                assert path.is_relative_to(pack)
                assert hashlib.sha256(path.read_bytes()).hexdigest() == record['sha256'], path
                checked += 1
        print(f'源文件散列：{checked} 条全部一致（只读检查）。')
    if runtime_report:
        results = json.loads(runtime_report.read_text('utf-8'))
        assert len(results) == 74 and {row['id'] for row in results} == ids
        failures = [row['id'] for row in results if not row['passed'] or not row['normalSpeed'] or row['pixelsPeak'] <= 0]
        assert not failures, failures
        print('74 项正常速度运行、画面像素与停止清理检查通过；不等同于原作视觉一致。')
    print(f'资源检查通过：74 场景 / {particles} 粒子系统 / 41 源 Shader / {len(visited)} 个运行时引用。')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, help='可选的只读 Effects 源目录')
    parser.add_argument('--runtime-report', type=Path, help='预览 -Verify 生成的 results.json')
    args = parser.parse_args()
    validate(args.source, args.runtime_report)
