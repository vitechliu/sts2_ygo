const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const config = require('../fmod-runtime.json');
const directory = path.resolve(__dirname, '../vendor/fmod', config.version);
const verified = new Map();

function runtimeFile(name) {
    const expected = config.files.find(file => file.name === name);
    if (!expected) throw new Error('未知的 FMOD 运行库文件。');
    const file = path.join(directory, name);
    let stat;
    try { stat = fs.statSync(file); }
    catch { throw new Error('缺少 FMOD WASM 依赖，请在 Web 目录运行 npm run prepare:fmod。'); }
    const signature = `${stat.size}:${stat.mtimeMs}`;
    if (verified.get(file) !== signature) {
        const hash = crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
        if (stat.size !== expected.bytes || hash !== expected.sha256)
            throw new Error('FMOD WASM 运行库校验失败，请运行 npm run prepare:fmod 修复。');
        verified.set(file, signature);
    }
    return file;
}
function runtimeInfo() {
    config.files.forEach(file => runtimeFile(file.name));
    return { version: config.version, files: config.files.map(({ name, sha256 }) => ({
        name, url: `/api/ui-audio/runtime/${name}`, sha256
    })) };
}
module.exports = { runtimeFile, runtimeInfo, config };
