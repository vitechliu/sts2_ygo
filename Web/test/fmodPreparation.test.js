const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs/promises');
const path = require('path');
const os = require('os');
const crypto = require('crypto');
const { prepare } = require('../scripts/prepare-fmod');

test('FMOD 准备校验整套文件、重复复用、下载失败保留旧目录且可重试', async () => {
    const directory = await fs.mkdtemp(path.join(os.tmpdir(), 'vygo-fmod-'));
    const config = { version: '2.03.08', files: ['fmodstudio.js', 'fmodstudio.wasm'].map(name => {
        const bytes = Buffer.from(`测试运行库：${name}`);
        return { name, url: `https://www.fmod.com/${name}`, bytes: bytes.length, sha256: crypto.createHash('sha256').update(bytes).digest('hex') };
    }) };
    let requests = 0;
    const downloadFile = async (file, destination) => { requests++; await fs.writeFile(destination, `测试运行库：${file.name}`); };
    const options = { directory, config, downloadFile, log: () => {} };
    try {
        assert.equal((await prepare(options)).reused, false);
        assert.equal(requests, 2);
        assert.equal((await prepare(options)).reused, true);
        assert.equal(requests, 2);
        const jsPath = path.join(directory, config.version, 'fmodstudio.js');
        await fs.writeFile(jsPath, '损坏的旧文件');
        await assert.rejects(prepare({ ...options, downloadFile: async () => { throw new Error('模拟下载中断'); } }), /下载中断/);
        assert.equal(await fs.readFile(jsPath, 'utf8'), '损坏的旧文件');
        assert.deepEqual(await fs.readdir(directory), [config.version]);
        await assert.rejects(prepare({ ...options, downloadFile: async (_, destination) => fs.writeFile(destination, '摘要不符') }), /完整性/);
        assert.equal(await fs.readFile(jsPath, 'utf8'), '损坏的旧文件');
        assert.deepEqual(await fs.readdir(directory), [config.version]);
        assert.equal((await prepare(options)).reused, false);
        assert.equal(await fs.readFile(jsPath, 'utf8'), '测试运行库：fmodstudio.js');
        await assert.rejects(prepare({ ...options, config: { ...config, version: '../越界' } }), /版本配置/);
    } finally { await fs.rm(directory, { recursive: true, force: true }); }
});
