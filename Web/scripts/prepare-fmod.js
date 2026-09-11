const fs = require('fs/promises');
const https = require('https');
const path = require('path');
const crypto = require('crypto');
const pinned = require('../fmod-runtime.json');

const defaultDirectory = path.resolve(__dirname, '../vendor/fmod');
const sha256 = bytes => crypto.createHash('sha256').update(bytes).digest('hex');

function validateConfig(config) {
    if (!/^\d+\.\d+\.\d+$/.test(config.version) || !Array.isArray(config.files) || config.files.length !== 2)
        throw new Error('FMOD 版本配置无效。');
    const names = new Set(config.files.map(file => file.name));
    if (!names.has('fmodstudio.js') || !names.has('fmodstudio.wasm')) throw new Error('必须配置配套的 JS 和 WASM。');
    for (const file of config.files) {
        const url = new URL(file.url);
        if (url.protocol !== 'https:' || !['www.fmod.com', 'fmod.com'].includes(url.hostname)
            || !/^[a-f0-9]{64}$/.test(file.sha256) || !Number.isSafeInteger(file.bytes) || file.bytes <= 0)
            throw new Error(`FMOD 来源或完整性配置无效：${file.name}`);
    }
}

async function validFile(filePath, expected) {
    try {
        const info = await fs.lstat(filePath);
        if (!info.isFile() || info.size !== expected.bytes) return false;
        return sha256(await fs.readFile(filePath)) === expected.sha256;
    } catch (error) {
        if (error.code === 'ENOENT') return false;
        throw error;
    }
}

function download(expected, destination, address = expected.url, redirects = 0) {
    return new Promise((resolve, reject) => {
        const url = new URL(address);
        if (url.protocol !== 'https:' || !['www.fmod.com', 'fmod.com'].includes(url.hostname)) {
            reject(new Error('下载跳转到了未配置的来源，已停止。')); return;
        }
        const request = https.get(url, response => {
            if ([301, 302, 303, 307, 308].includes(response.statusCode)) {
                response.resume();
                if (redirects >= 4 || !response.headers.location) return reject(new Error('官方资源重定向异常。'));
                download(expected, destination, new URL(response.headers.location, url).href, redirects + 1).then(resolve, reject);
                return;
            }
            if (response.statusCode !== 200) {
                response.resume(); reject(new Error(`官方资源返回 HTTP ${response.statusCode}：${expected.name}`)); return;
            }
            const chunks = [];
            let size = 0;
            response.on('data', chunk => {
                size += chunk.length;
                if (size > expected.bytes) {
                    const error = new Error(`${expected.name} 大小与固定版本不符，官方资源可能已更新。`);
                    reject(error);
                    response.destroy(error);
                } else chunks.push(chunk);
            });
            response.on('error', reject);
            response.on('aborted', () => reject(new Error(`${expected.name} 下载中断。`)));
            response.on('end', () => {
                const bytes = Buffer.concat(chunks);
                if (bytes.length !== expected.bytes || sha256(bytes) !== expected.sha256) {
                    reject(new Error(`${expected.name} 完整性校验失败，拒绝安装。官方资源可能已更新或下载不完整。`)); return;
                }
                fs.writeFile(destination, bytes, { flag: 'wx' }).then(resolve, reject);
            });
        });
        request.on('error', reject);
        request.setTimeout(30000, () => request.destroy(new Error(`${expected.name} 下载超时。`)));
    });
}

function within(directory, name) {
    const target = path.resolve(directory, name);
    if (!target.startsWith(path.resolve(directory) + path.sep)) throw new Error('依赖目录越界。');
    return target;
}

async function prepare({ directory = defaultDirectory, config = pinned, downloadFile = download, log = console.log } = {}) {
    validateConfig(config);
    const target = within(directory, config.version);
    const matches = await Promise.all(config.files.map(file => validFile(path.join(target, file.name), file)));
    if (matches.every(Boolean)) {
        log(`FMOD ${config.version} 已通过完整性校验，复用本地运行库。`);
        return { reused: true, directory: target };
    }
    await fs.mkdir(directory, { recursive: true });
    const staging = within(directory, `.prepare-${crypto.randomUUID()}`);
    const backup = within(directory, `.previous-${crypto.randomUUID()}`);
    await fs.mkdir(staging);
    let movedPrevious = false;
    try {
        for (const file of config.files) {
            log(`正在从官方来源准备 ${file.name}（FMOD ${config.version}）…`);
            const temporary = path.join(staging, file.name + '.download');
            await downloadFile(file, temporary);
            if (!await validFile(temporary, file)) throw new Error(`${file.name} 完整性校验失败。`);
            await fs.rename(temporary, path.join(staging, file.name));
        }
        // 整套文件准备成功后才替换目录，下载失败不会损坏既有运行库。
        try { await fs.rename(target, backup); movedPrevious = true; }
        catch (error) { if (error.code !== 'ENOENT') throw error; }
        try { await fs.rename(staging, target); }
        catch (error) {
            if (movedPrevious) { await fs.rename(backup, target); movedPrevious = false; }
            throw error;
        }
        if (movedPrevious) await fs.rm(backup, { recursive: true, force: true });
        log(`FMOD ${config.version} 已准备并校验完成：${target}`);
        return { reused: false, directory: target };
    } finally {
        await fs.rm(staging, { recursive: true, force: true });
    }
}

if (require.main === module) {
    prepare().catch(error => {
        console.error(`FMOD 运行库准备失败：${error.message}\n请检查网络后在 Web 目录运行 npm run prepare:fmod 重试。若官方文件发生变化，需重新验证版本并更新 fmod-runtime.json，不能跳过校验。`);
        process.exitCode = 1;
    });
}

module.exports = { prepare, download, validateConfig };
