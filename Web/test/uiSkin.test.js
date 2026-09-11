const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs');
const os = require('os');
const path = require('path');
const sharp = require('sharp');
const express = require('express');
const { scan, originalImage } = require('../services/uiSkin/catalog');
const { UiSkinService, render } = require('../services/uiSkin/service');
const { createRouter } = require('../routes/uiSkin');

async function fixture(t) {
    const root = fs.mkdtempSync(path.join(os.tmpdir(), 'vygo-ui-test-'));
    t.after(() => fs.rmSync(root, { recursive: true, force: true }));
    const vanilla = path.join(root, 'vanilla');
    function write(file, data) { const target = path.join(vanilla, file); fs.mkdirSync(path.dirname(target), { recursive: true }); fs.writeFileSync(target, data); }
    const png = await sharp({ create: { width: 16, height: 12, channels: 4, background: '#ff0000' } }).png().toBuffer();
    write('images/atlases/ui_atlas_0.png', png);
    write('images/direct.png', png);
    write('images/atlases/ui_atlas.tpsheet', JSON.stringify({ textures: [{ image: 'ui_atlas_0.png', sprites: [{ filename: 'confirm_button.png', region: { x: 2, y: 1, w: 8, h: 6 }, margin: { x: 2, y: 3, w: 5, h: 4 } }] }] }));
    write('scenes/ui/confirm_button.tscn', `[gd_scene format=3]
[ext_resource type="Texture2D" path="res://images/atlases/ui_atlas.sprites/confirm_button.tres" id="1"]
[node name="Confirm" type="Control"]
[node name="Image" type="TextureRect" parent="."]
texture = ExtResource("1")
expand_mode = 1
`);
    write('scenes/ui/hover_tip.tscn', `[gd_scene format=3]
[ext_resource type="Texture2D" path="res://images/direct.png" id="1"]
[node name="Tip" type="MarginContainer"]
[node name="Bg" type="NinePatchRect" parent="."]
texture = ExtResource("1")
patch_margin_left = 2
`);
    write('scenes/ui/settings_screen_popup.tscn', `[gd_scene format=3]
[ext_resource type="Texture2D" path="res://images/atlases/ui_atlas_0.png" id="1"]
[sub_resource type="AtlasTexture" id="atlas"]
atlas = ExtResource("1")
region = Rect2(2, 1, 8, 6)
margin = Rect2(2, 3, 5, 4)
[node name="Popup" type="TextureRect"]
texture = SubResource("atlas")
`);
    write('scenes/test.tscn', `[gd_scene format=3]
[ext_resource type="PackedScene" path="res://scenes/ui/confirm_button.tscn" id="1"]
[node name="Screen" type="Control"]
[node name="Confirm" parent="." instance=ExtResource("1")]
`);
    const service = new UiSkinService({ projectRoot: root, vanillaRoot: vanilla });
    const catalog = await service.scan();
    const source = await service.importImage(png, '测试原图.png');
    const rule = { id: 'test-rule', componentId: 'confirm', slotId: 'Image|texture', scope: 'component', states: { normal: { source: source.id, mode: 'contain', crop: [0, 0, 16, 12] } } };
    return { root, vanilla, service, catalog, source, rule, write };
}

test('扫描命名、内嵌和独立纹理，并展开场景实例引用', async t => {
    const { catalog } = await fixture(t);
    assert.equal(catalog.components.find(c => c.id === 'confirm').slots[0].usages.length, 2);
    assert(catalog.assets.some(a => a.id.includes('::atlas')));
    assert(catalog.assets.some(a => a.id === 'res://images/direct.png'));
    const asset = catalog.assets.find(a => a.name === 'confirm_button.png');
    assert.deepEqual([asset.width, asset.height], [13, 10]);
    assert.deepEqual(asset.margin, [2, 3, 5, 4]);
});
test('原图恢复正确的透明画布和内容偏移', async t => {
    const { catalog, vanilla } = await fixture(t);
    const asset = catalog.assets.find(a => a.name === 'confirm_button.png');
    const { data, info } = await sharp(await originalImage(vanilla, asset)).raw().toBuffer({ resolveWithObject: true });
    assert.deepEqual([info.width, info.height], [13, 10]);
    assert.equal(data[3], 0);
    assert.equal(data[(3 * 13 + 2) * 4 + 3], 255);
    assert.equal(data[(9 * 13 + 12) * 4 + 3], 0);
});
test('场景覆盖引用使用自身资源表而不是被实例化场景的 ID', async t => {
    const { write, vanilla } = await fixture(t);
    write('scenes/test.tscn', `[gd_scene format=3]
[ext_resource type="PackedScene" path="res://scenes/ui/confirm_button.tscn" id="1"]
[ext_resource type="Texture2D" path="res://images/direct.png" id="2"]
[node name="Screen" type="Control"]
[node name="Confirm" parent="." instance=ExtResource("1")]
[node name="Image" parent="Confirm"]
texture = ExtResource("2")
`);
    const catalog = await scan(vanilla);
    assert.equal(catalog.components.find(c => c.id === 'confirm').slots[0].usages.length, 1);
    assert(catalog.assets.find(a => a.id === 'res://images/direct.png').refs.some(r => r.scene === 'res://scenes/test.tscn' && r.nodePath === 'Confirm/Image'));
});
test('九宫格扩展保持角落像素和固定宽度', async t => {
    const { service } = await fixture(t);
    const pixels = Buffer.alloc(8 * 8 * 4);
    for (let y = 0; y < 8; y++) for (let x = 0; x < 8; x++) {
        const p = (y * 8 + x) * 4; pixels[p] = x < 2 && y < 2 ? 255 : 0; pixels[p + 1] = x >= 2 || y >= 2 ? 255 : 0; pixels[p + 3] = 255;
    }
    const source = await service.importImage(await sharp(pixels, { raw: { width: 8, height: 8, channels: 4 } }).png().toBuffer());
    const spec = { source: source.id, mode: 'nine', border: [2, 2, 2, 2] };
    const result = await render(service.base, spec, [40, 24]);
    const raw = await sharp(result.png).raw().toBuffer();
    assert.equal(raw[(1 * 40 + 1) * 4], 255); assert.equal(raw[(1 * 40 + 2) * 4], 0);
    assert.deepEqual([result.width, result.height], [40, 24]);
    const exported = await render(service.base, spec);
    assert.deepEqual([exported.width, exported.height], [8, 8]);
});
test('等比留白、覆盖和偏移产生预期透明区域', async t => {
    const { service, source } = await fixture(t);
    const contain = await render(service.base, { source: source.id, mode: 'contain' }, [16, 24]);
    const raw = await sharp(contain.png).raw().toBuffer();
    assert.equal(raw[3], 0); assert.equal(raw[(6 * 16) * 4 + 3], 255);
    const cover = await render(service.base, { source: source.id, mode: 'cover' }, [16, 24]);
    assert.equal((await sharp(cover.png).raw().toBuffer())[3], 255);
    await assert.rejects(render(service.base, { source: source.id, offset: [4096, 4096] }, [16, 24]), /完全位于/);
});
test('拒绝裁剪越界、边框重叠和不同状态输出尺寸', async t => {
    const { service, rule, source } = await fixture(t);
    await assert.rejects(render(service.base, { source: source.id, crop: [15, 0, 8, 8] }, [20, 20]), /越界/);
    await assert.rejects(render(service.base, { source: source.id, mode: 'nine', border: [8, 0, 8, 0] }), /重叠/);
    rule.states.normal.mode = 'nine';
    rule.states.hover = { source: source.id, mode: 'nine', crop: [0, 0, 10, 10] };
    await assert.rejects(service.save({ schemaVersion: 1, rules: [rule] }), /尺寸/);
});
test('共用规则和局部覆盖可共存，同级重复映射被拒绝', async t => {
    const { service, rule } = await fixture(t);
    const local = { ...structuredClone(rule), id: 'local', scope: 'scene', scene: 'res://scenes/test.tscn', nodePath: 'Confirm/Image' };
    await service.save({ schemaVersion: 1, rules: [rule, local] });
    await assert.rejects(service.save({ schemaVersion: 1, rules: [rule, { ...rule, id: 'duplicate' }] }), /重复映射/);
    await assert.rejects(service.save({ schemaVersion: 1, rules: [local, { ...local, id: 'duplicate' }] }), /重复映射/);
});
test('导出只含可移植资源，内容寻址复用图片，失败保留旧清单', async t => {
    const { service, rule, root } = await fixture(t);
    await service.save({ schemaVersion: 1, rules: [rule] });
    const result = await service.export(); assert.equal(result.images, 1);
    const file = path.join(service.output, 'skin.json'), before = fs.readFileSync(file, 'utf8');
    assert(!before.includes(root)); assert(!before.includes('sources/'));
    const manifest = JSON.parse(before);
    assert.equal(manifest.rules[0].selectors.length, 2);
    const source = path.join(service.base, 'sources', rule.states.normal.source);
    fs.unlinkSync(source);
    await assert.rejects(service.export()); assert.equal(fs.readFileSync(file, 'utf8'), before);
});
test('原版扫描指纹变化要求重新核对保存', async t => {
    const { service, rule, write } = await fixture(t);
    await service.save({ schemaVersion: 1, rules: [rule] });
    write('scenes/new.tscn', '[gd_scene format=3]\n[node name="New" type="Control"]\n');
    await service.scan(); await assert.rejects(service.export(), /原版资源已变化/);
    await service.save({ schemaVersion: 1, rules: [rule] }); await service.export();
});
test('拒绝路径遍历和符号链接素材', async t => {
    const { service, root } = await fixture(t);
    await assert.rejects(render(service.base, { source: '../../secret.png' }, [8, 8]), /标识/);
    const id = 'a'.repeat(64) + '.png', outside = path.join(root, 'outside.png'); fs.writeFileSync(outside, 'secret');
    fs.symlinkSync(outside, path.join(service.base, 'sources', id));
    await assert.rejects(render(service.base, { source: id }, [8, 8]), /越界/);
});
test('拾取记录可回到工作台并导出节点规则', async t => {
    const { service } = await fixture(t);
    const catalog = await service.capture({ schemaVersion: 1, type: 'TextureRect', contexts: [{ scene: 'res://scenes/test.tscn', nodePath: 'DynamicImage' }], textures: { texture: { path: 'res://images/direct.png' } } });
    assert.equal(catalog.components.at(-1).category, '拾取节点');
    assert.equal(catalog.components.at(-1).slots[0].usages[0].nodePath, 'DynamicImage');
    await service.scan(); assert.equal(service.getCatalog().components.at(-1).category, '拾取节点');
});
test('平铺导出保留原单元尺寸，预览可以比单元小', async t => {
    const { service, source } = await fixture(t);
    const spec = { source: source.id, mode: 'tile' };
    assert.deepEqual([...(await render(service.base, spec)).border], [0, 0, 0, 0]);
    const small = await render(service.base, spec, [4, 4]); assert.deepEqual([small.width, small.height], [4, 4]);
    const native = await render(service.base, spec); assert.deepEqual([native.width, native.height], [16, 12]);
});
test('HTTP 导入、预览、配置导出流程和跨站写入拦截', async t => {
    const { service, rule } = await fixture(t);
    const app = express(); app.use(express.json()); app.use('/api/ui-skin', createRouter(service));
    const server = app.listen(0, '127.0.0.1'); await new Promise(resolve => server.once('listening', resolve));
    t.after(() => new Promise(resolve => { server.closeAllConnections(); server.close(resolve); }));
    const base = `http://127.0.0.1:${server.address().port}/api/ui-skin`;
    const put = body => fetch(`${base}/config`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
    assert.equal((await put({ schemaVersion: 1, rules: [rule] })).status, 200);
    const preview = await fetch(`${base}/preview`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ rule }) });
    assert.equal(preview.status, 200); assert.match(preview.headers.get('content-type'), /image\/png/);
    assert.equal((await fetch(`${base}/export`, { method: 'POST' })).status, 200);
    assert.equal((await fetch(`${base}/export`, { method: 'POST', headers: { Origin: 'https://unrelated.example' } })).status, 403);
});

test('自动收录未预置界面，并把卡牌图鉴纯色背景作为可替换图层导出', async t => {
    const { write, service, source } = await fixture(t);
    const scene = 'res://scenes/screens/card_library/card_library.tscn';
    write('scenes/screens/card_library/card_library.tscn', `[gd_scene format=3]
[node name="Library" type="Control"]
[node name="Panel" type="ColorRect" parent="."]
custom_minimum_size = Vector2(288, 80)
color = Color(0.2, 0.4, 0.6, 0.5)
`);
    write('scenes/ui/new_panel.tscn', `[gd_scene format=3]
[ext_resource type="Texture2D" path="res://images/direct.png" id="1"]
[node name="NewPanel" type="TextureRect"]
texture = ExtResource("1")
`);
    const catalog = await service.scan();
    assert(catalog.components.some(c => c.scene === 'res://scenes/ui/new_panel.tscn' && c.slots.length === 1));
    const component = catalog.components.find(c => c.id === 'card-library');
    const slot = component.slots.find(s => s.property === 'color');
    const asset = catalog.assets.find(a => a.id === slot.assetId);
    const { data, info } = await sharp(await originalImage(service.root, asset)).raw().toBuffer({ resolveWithObject: true });
    assert.deepEqual([info.width, info.height], [288, 80]);
    assert.deepEqual([...data.subarray(0, 4)], [51, 102, 153, 128]);
    await service.save({ schemaVersion: 1, rules: [{ id: 'background', componentId: component.id, slotId: slot.id, scope: 'component', states: { normal: { source: source.id, mode: 'nine', border: [2, 2, 2, 2] } } }] });
    await service.export();
    const manifest = JSON.parse(fs.readFileSync(path.join(service.output, 'skin.json')));
    assert.equal(manifest.rules[0].property, 'color');
    assert.deepEqual(manifest.rules[0].texture.color, [0.2, 0.4, 0.6, 0.5]);
    assert.deepEqual(manifest.rules[0].selectors, [{ scene, nodePath: 'Panel' }]);
    assert.equal(manifest.rules[0].texture.path, '');
});

test('其他 Atlas 与动态统计图标可独立映射，重复变体仍拒绝', async t => {
    const { write, service, source } = await fixture(t);
    write('images/atlases/stats_screen_atlas.tpsheet', JSON.stringify({ textures: [{ image: 'ui_atlas_0.png', sprites: ['clock', 'cards'].map((name, i) => ({ filename: `stats_${name}.png`, region: { x: i * 4, y: 0, w: 4, h: 4 } })) }] }));
    write('scenes/screens/stats_screen/stats_screen_section.tscn', `[gd_scene format=3]
[node name="Stats" type="Control"]
[node name="HBoxContainer" type="HBoxContainer" parent="."]
[node name="Icon" type="TextureRect" parent="HBoxContainer"]
`);
    const catalog = await service.scan();
    const rules = ['clock', 'cards'].map(name => {
        const c = catalog.components.find(c => c.id === `stats-icon-${name}`);
        assert(c.slots[0].textureVariant);
        return { id: name, componentId: c.id, slotId: c.slots[0].id, scope: 'component', states: { normal: { source: source.id, mode: 'contain' } } };
    });
    await service.save({ schemaVersion: 1, rules });
    await service.export();
    const manifest = JSON.parse(fs.readFileSync(path.join(service.output, 'skin.json')));
    assert.equal(manifest.rules.length, 2);
    assert(manifest.rules.every(r => r.textureVariant));
    assert.notEqual(manifest.rules[0].texture.path, manifest.rules[1].texture.path);
    await assert.rejects(service.save({ schemaVersion: 1, rules: [...rules, { ...rules[0], id: 'duplicate' }] }), /重复映射/);
});

test('百科入口采用源码规定的动态图片路径，并保留不存在时的场景原图', async t => {
    const { write, service } = await fixture(t);
    write('scenes/screens/compendium_submenu.tscn', `[gd_scene format=3]
[ext_resource type="Texture2D" path="res://images/direct.png" id="1"]
[node name="Compendium" type="Control"]
[node name="Buttons" type="Control" parent="."]
[node name="CardLibraryButton" type="Control" parent="Buttons"]
[node name="Icon" type="TextureRect" parent="Buttons/CardLibraryButton"]
texture = ExtResource("1")
[node name="RelicCollectionButton" type="Control" parent="Buttons"]
[node name="Icon" type="TextureRect" parent="Buttons/RelicCollectionButton"]
texture = ExtResource("1")
`);
    const id = 'res://images/packed/main_menu/submenu_icon_compendium_card_library.png';
    write(id.slice(6), await sharp({ create: { width: 20, height: 20, channels: 4, background: '#00ff00' } }).png().toBuffer());
    const catalog = await service.scan(), c = catalog.components.find(c => c.id === 'compendium');
    assert.equal(c.slots.find(s => s.nodePath.includes('CardLibraryButton')).assetId, id);
    assert.equal(c.slots.find(s => s.nodePath.includes('RelicCollectionButton')).assetId, 'res://images/direct.png');
    assert(catalog.assets.find(a => a.id === id).refs.length > 0);
});

test('空白映射无需素材即可保存、预览和导出，缺失状态也回退透明图', async t => {
    const { service, rule } = await fixture(t);
    fs.rmSync(path.join(service.base, 'sources'), { recursive: true });
    rule.states = { normal: { blank: true } };
    await service.save({ schemaVersion: 1, rules: [rule] });
    assert.deepEqual(service.config().rules[0].states.normal, { blank: true });
    const preview = await service.preview(rule, 'hover');
    assert.deepEqual([preview.width, preview.height], [13, 10]);
    const raw = await sharp(preview.png).ensureAlpha().raw().toBuffer();
    assert(raw.every((value, i) => i % 4 !== 3 || value === 0));
    const result = await service.export();
    assert.deepEqual([result.rules, result.images], [1, 1]);
    const manifest = JSON.parse(fs.readFileSync(path.join(service.output, 'skin.json')));
    const image = path.join(service.projectRoot, manifest.rules[0].states.normal.slice(6));
    assert(fs.existsSync(image));
    assert.deepEqual(fs.readFileSync(image), preview.png);
    assert(!fs.existsSync(path.join(service.base, 'sources')));
    const second = await service.export();
    assert.equal(second.images, 1);
    assert.equal(fs.readdirSync(path.join(service.output, 'generated')).length, 1);
});

test('空白状态兼容九宫格素材尺寸与边框，也允许正常为空白而悬停显示图片', async t => {
    const { service, source, rule } = await fixture(t);
    const art = { source: source.id, mode: 'nine', border: [2, 3, 2, 3] };
    for (const states of [{ normal: art, hover: { blank: true } }, { normal: { blank: true }, hover: art }]) {
        rule.states = states;
        await service.save({ schemaVersion: 1, rules: [rule] });
        const blankState = states.normal.blank ? 'normal' : 'hover';
        const preview = await service.preview(rule, blankState, [300, 80]);
        assert.deepEqual([preview.width, preview.height], [300, 80]);
        await service.export();
        const manifest = JSON.parse(fs.readFileSync(path.join(service.output, 'skin.json')));
        assert.equal(manifest.rules[0].mode, 'nine');
        assert.deepEqual(manifest.rules[0].border, [2, 3, 2, 3]);
        for (const file of Object.values(manifest.rules[0].states)) {
            const metadata = await sharp(path.join(service.projectRoot, file.slice(6))).metadata();
            assert.deepEqual([metadata.width, metadata.height], [16, 12]);
        }
    }
    rule.states = { normal: { blank: true, source: source.id } };
    await assert.rejects(service.save({ schemaVersion: 1, rules: [rule] }), /不能同时指定素材/);
    rule.states = { normal: { blank: 'true' } };
    await assert.rejects(service.save({ schemaVersion: 1, rules: [rule] }), /状态素材配置无效/);
});
