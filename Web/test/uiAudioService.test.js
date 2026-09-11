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

test('WASM 管理链路：事件枚举、工程导入更新、播放清单和不兼容反馈', {
    skip: process.env.VYGO_AUDIO_INTEGRATION !== '1', timeout: 120000
}, async () => {
    fs.writeFileSync(manifest, JSON.stringify({ version: 1, profiles: [], mappings: {} }));
    const originalBanks = ['Master.bank', 'Master.strings.bank', 'sfx.bank'].map(name => path.join(process.env.FMOD_ORIGINAL_BANK_DIR, name));
    const inspected = await service.inspectBanks({ banks: originalBanks });
    assert.equal(inspected.version, '2.03.08');
    for (const item of service.state().catalog) assert.ok(inspected.events.some(event => event.path === item.path), item.path);
    assert.deepEqual(Object.keys(service.state().settings), ['originalBankDir']);
    const input = {
        name: '真实 VYgo bank 测试', bankDir: path.resolve(__dirname, '../../VYgo/banks'),
        bankFiles: ['VYgo.bank'], guidFile: path.resolve(__dirname, '../../VYgo/banks/VYgo.guids.txt')
    };
    const profile = await service.createProfile(input);
    const event = profile.events.find(item => item.path === 'event:/vygo/sfx/material_shine');
    assert.ok(event);
    const result = service.playback({ profileId: profile.id, event: event.path });
    assert.equal(result.guid, event.guid);
    assert.equal(result.runtime.version, '2.03.08');
    for (const bank of result.banks) assert.ok(fs.existsSync(service.bankFile(bank.id)));
    assert.throws(() => service.bankFile('../Master.bank'), /标识/);
    service.saveMapping({ source, profileId: profile.id, event: event.path });
    const updated = await service.createProfile({ ...input, name: '更新后的工程' }, profile.id);
    assert.equal(updated.id, profile.id);
    assert.equal(service.state().mappings[source].event, event.path);
    assert.equal(service.state().profiles.length, 1);
    await assert.rejects(service.createProfile({ ...input, bankFiles: ['missing.bank'] }), /ENOENT/);
    assert.throws(() => service.playback({ event: 'event:/sfx/ui/relic_activate_general' }), /清单/);
    const incompatible = path.join(root, 'invalid.bank');
    fs.writeFileSync(incompatible, Buffer.from('不兼容的 bank 内容'));
    await assert.rejects(service.inspectBanks({ banks: [incompatible] }), /bank 版本/);
    service.saveMapping({ source, profileId: null });
    assert.deepEqual(service.state().mappings, {});
});
