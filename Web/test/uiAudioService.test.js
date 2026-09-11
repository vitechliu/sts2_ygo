const { test, after } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs');
const path = require('path');
const os = require('os');

// 在独立目录验证持久化，绝不修改开发者正在编辑的替换方案。
const root = fs.mkdtempSync(path.join(os.tmpdir(), 'vygo-ui-audio-'));
process.env.VYGO_UI_AUDIO_ROOT = root;
fs.mkdirSync(path.join(root, 'VYgo/audio'), { recursive: true });
const manifest = path.join(root, 'VYgo/audio/ui-replacements.json');
const source = 'event:/sfx/ui/clicks/ui_click';
const target = 'event:/custom/ui/click';
fs.writeFileSync(manifest, JSON.stringify({ version: 1, profiles: [{ id: '测试', events: [{ path: target }, { path: 'event:/custom/music/menu' }] }], mappings: {} }));
const service = require('../services/uiAudioService');
after(() => fs.rmSync(root, { recursive: true, force: true }));

test('仅允许 19 个指定事件，保存、编辑、取消能跨读取持久化', () => {
    assert.equal(service.state().catalog.length, 19);
    assert.deepEqual(Object.values(service.state().catalog.reduce((result, item) => {
        result[item.group] = (result[item.group] || 0) + 1; return result;
    }, {})), [7, 5, 7]);
    service.saveMapping({ source, profileId: '测试', event: target });
    assert.deepEqual(JSON.parse(fs.readFileSync(manifest)).mappings[source], { profileId: '测试', event: target });
    assert.throws(() => service.saveMapping({ source: 'event:/sfx/ui/gain_energy', profileId: '测试', event: target }), /清单/);
    assert.throws(() => service.saveMapping({ source, profileId: '测试', event: 'event:/missing' }), /不存在/);
    assert.throws(() => service.saveMapping({ source, profileId: '测试', event: 'event:/custom/music/menu' }), /背景音乐/);
    service.saveMapping({ source, profileId: null });
    assert.deepEqual(service.state().mappings, {});
});

test('真实 FMOD：19 项原版、工程导入更新、自定义试听和缺依赖反馈', {
    skip: process.env.VYGO_AUDIO_INTEGRATION !== '1', timeout: 120000
}, async () => {
    fs.writeFileSync(manifest, JSON.stringify({ version: 1, profiles: [], mappings: {} }));
    fs.mkdirSync(path.join(root, 'Web/scripts'), { recursive: true });
    fs.copyFileSync(path.join(__dirname, '../scripts/fmod-preview.py'), path.join(root, 'Web/scripts/fmod-preview.py'));
    for (const item of service.state().catalog) {
        const result = await service.preview({ event: item.path });
        assert.ok(result.peak > 1, item.path);
        assert.ok(result.duration > 0 && result.duration < 9, item.path);
        assert.equal(fs.readFileSync(result.output).toString('ascii', 0, 4), 'RIFF');
        fs.rmSync(result.output);
        console.log(`${item.label}：${result.duration.toFixed(2)} 秒，峰值 ${result.peak}`);
    }
    const input = {
        name: '真实 VYgo bank 测试', bankDir: path.resolve(__dirname, '../../VYgo/banks'),
        bankFiles: ['VYgo.bank'], guidFile: path.resolve(__dirname, '../../VYgo/banks/VYgo.guids.txt')
    };
    const profile = await service.createProfile(input);
    const event = profile.events.find(item => item.path === 'event:/vygo/sfx/material_shine');
    assert.ok(event);
    const result = await service.preview({ profileId: profile.id, event: event.path });
    assert.ok(result.peak > 1);
    console.log(`替换事件：${result.duration.toFixed(2)} 秒，峰值 ${result.peak}`);
    service.saveMapping({ source, profileId: profile.id, event: event.path });
    const updated = await service.createProfile({ ...input, name: '更新后的工程' }, profile.id);
    assert.equal(updated.id, profile.id);
    assert.equal(service.state().mappings[source].event, event.path);
    assert.equal(service.state().profiles.length, 1);
    await assert.rejects(service.createProfile({ ...input, bankFiles: ['missing.bank'] }), /ENOENT/);
    await assert.rejects(service.preview({ event: 'event:/sfx/ui/relic_activate_general' }), /清单/);
    service.saveMapping({ source, profileId: null });
    assert.deepEqual(service.state().mappings, {});
});
