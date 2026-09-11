#!/usr/bin/env python3
"""在临时 Godot 工程中验证真实皮肤源码；不写游戏设置或正式皮肤。"""
import argparse
import pathlib
import shutil
import struct
import subprocess
import tempfile
import xml.etree.ElementTree as ET
import zlib

parser = argparse.ArgumentParser()
parser.add_argument('--project', type=pathlib.Path, default=pathlib.Path(__file__).resolve().parents[2])
parser.add_argument('--godot')
args = parser.parse_args()
env = ET.parse(args.project / 'env.props').getroot().find('PropertyGroup')
props = {element.tag: element.text for element in env}
for _ in range(3):
    for key, value in props.items():
        for other, replacement in props.items():
            value = value.replace('$(' + other + ')', replacement)
        props[key] = value
root = pathlib.Path(tempfile.mkdtemp(prefix='vygo-ui-runtime-')).resolve()
print(root, flush=True)
def png(name, rgba):
    def chunk(kind, data):
        return struct.pack('!I', len(data)) + kind + data + struct.pack('!I', zlib.crc32(kind + data) & 0xffffffff)
    image = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('!2I5B', 16, 12, 8, 6, 0, 0, 0))
    image += chunk(b'IDAT', zlib.compress((b'\x00' + bytes(rgba) * 16) * 12)) + chunk(b'IEND', b'')
    file = root / name
    file.parent.mkdir(parents=True, exist_ok=True)
    file.write_bytes(image)
png('original.png', [200, 20, 20, 255])
for name, color in [('normal', [20, 200, 20, 255]), ('hover', [20, 20, 200, 255]), ('pressed', [200, 200, 20, 255])]:
    png('VYgo/ui_skin/generated/' + name + '.png', color)
(root / 'Main.cs').write_text((pathlib.Path(__file__).parent / 'Main.cs.txt').read_text())
(root / 'project.godot').write_text('''config_version=5
[application]
config/name="VYgo UI runtime tests"
run/main_scene="res://main.tscn"
[dotnet]
project/assembly_name="UiSkinTests"
[rendering]
renderer/rendering_method="gl_compatibility"
''')
(root / 'main.tscn').write_text('''[gd_scene load_steps=2 format=3]
[ext_resource type="Script" path="res://Main.cs" id="1"]
[node name="Main" type="Node"]
script = ExtResource("1")
''')
# 按字节复制当前生产源码到临时工程，避免 Godot 为工程外文件生成失效脚本路径。
for source in (args.project / 'Core/UiSkin').glob('*.cs'):
    shutil.copy2(source, root / source.name)
extra_references = ''.join(f'<Reference Include="{p.stem}"><HintPath>{p}</HintPath></Reference>' for p in pathlib.Path(props['Sts2DataDir']).glob('*.dll') if p.stem not in ['sts2', '0Harmony', 'GodotSharp', 'GodotSharpEditor', 'mscorlib', 'netstandard', 'WindowsBase'] and not p.stem.startswith(('System', 'Microsoft')))
(root / 'UiSkinTests.csproj').write_text(f'''<Project Sdk="Godot.NET.Sdk/4.5.1">
<PropertyGroup><TargetFramework>net9.0</TargetFramework><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings><EnableDynamicLoading>true</EnableDynamicLoading></PropertyGroup>
<ItemGroup><Reference Include="sts2"><HintPath>{props['Sts2DllDir']}/sts2.dll</HintPath></Reference>
<Reference Include="0Harmony"><HintPath>{props['Sts2DataDir']}/0Harmony.dll</HintPath></Reference>
{extra_references}
</ItemGroup></Project>''')
godot = args.godot or props['GodotPath']
subprocess.run(['dotnet', 'build', str(root / 'UiSkinTests.csproj')], cwd=root, check=True)
subprocess.run([godot, '--headless', '--editor', '--path', str(root), '--import'], cwd=root, check=True, timeout=90)
subprocess.run([godot, '--headless', '--path', str(root), '--', 'skin-test'], cwd=root, check=True, timeout=45)
subprocess.run([godot, '--headless', '--path', str(root), '--', 'skin-disabled'], cwd=root, check=True, timeout=45)
