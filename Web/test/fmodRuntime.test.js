const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs');
const path = require('path');
const vm = require('vm');
const crypto = require('crypto');

test('提供运行库前拒绝缺失或内容损坏的依赖，修复后可重试', () => {
    const contents = new Map();
    let revision = 0;
    const config = { version: '2.03.08', files: ['fmodstudio.js', 'fmodstudio.wasm'].map(name => ({
        name, bytes: 4, sha256: crypto.createHash('sha256').update('正确').digest('hex')
    })) };
    // 在模块边界注入文件系统，不移动或破坏开发者实际安装的运行库。
    const sandbox = { module: { exports: {} }, __dirname: '/fixture/services', require: name => {
        if (name === 'fs') return {
            statSync(file) {
                const content = contents.get(path.basename(file));
                if (!content) throw new Error('ENOENT');
                return { size: content.length, mtimeMs: revision };
            },
            readFileSync: file => contents.get(path.basename(file))
        };
        if (name === '../fmod-runtime.json') return config;
        return require(name);
    } };
    const good = Buffer.from('正确');
    config.files.forEach(file => { file.bytes = good.length; });
    vm.runInNewContext(fs.readFileSync(path.resolve(__dirname, '../services/fmodRuntime.js'), 'utf8'), sandbox);
    const runtime = sandbox.module.exports;
    assert.throws(() => runtime.runtimeInfo(), /缺少.*prepare:fmod/);
    config.files.forEach(file => contents.set(file.name, good));
    assert.equal(runtime.runtimeInfo().version, config.version);
    contents.set('fmodstudio.js', Buffer.from('损坏')); revision++;
    assert.throws(() => runtime.runtimeInfo(), /校验失败/);
    contents.set('fmodstudio.js', good); revision++;
    assert.equal(runtime.runtimeInfo().files.length, 2);
    assert.throws(() => runtime.runtimeFile('../fmodstudio.js'), /未知/);
});
