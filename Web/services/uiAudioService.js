const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { spawn } = require('child_process');
const { runtimeInfo } = require('./fmodRuntime');

const ROOT = process.env.VYGO_UI_AUDIO_ROOT || path.resolve(__dirname, '../..');
const catalog = require('../../VYgo/audio/ui-events.json');
const allowed = new Set(catalog.map(item => item.path));
const manifestPath = path.join(ROOT, 'VYgo/audio/ui-replacements.json');
const settingsPath = path.join(ROOT, 'Web/audio-settings.local.json');
const originalFiles = ['Master.bank', 'Master.strings.bank', 'sfx.bank'];
const hashes = new Map();
let inspections = 0;

function readJson(file, fallback) {
    return fs.existsSync(file) ? JSON.parse(fs.readFileSync(file, 'utf8').replace(/^\uFEFF/, '')) : fallback;
}
function writeJson(file, value) {
    fs.mkdirSync(path.dirname(file), { recursive: true });
    const temp = `${file}.${crypto.randomUUID()}.tmp`;
    fs.writeFileSync(temp, JSON.stringify(value, null, 2) + '\n');
    fs.renameSync(temp, file);
}
function settings() {
    const saved = readJson(settingsPath, {});
    return { originalBankDir: saved.originalBankDir || process.env.FMOD_ORIGINAL_BANK_DIR || '' };
}
function originalBanks() {
    const { originalBankDir } = settings();
    if (!originalBankDir) throw new Error('请先保存原版 bank 目录。');
    return originalFiles.map(name => path.join(originalBankDir, name));
}
function state() {
    return { catalog, settings: settings(), ...readJson(manifestPath, { version: 1, profiles: [], mappings: {} }) };
}
function saveSettings(input) {
    if (typeof input.originalBankDir !== 'string' || !input.originalBankDir.trim()) throw new Error('请填写原版 bank 目录。');
    const config = { originalBankDir: path.resolve(input.originalBankDir.trim()) };
    for (const name of originalFiles) {
        const file = path.join(config.originalBankDir, name);
        if (!fs.existsSync(file) || !fs.statSync(file).isFile()) throw new Error(`缺少原版 bank：${name}`);
    }
    writeJson(settingsPath, config);
    return config;
}
function guidNames(file) {
    const names = {};
    if (!file) return names;
    for (const line of fs.readFileSync(file, 'utf8').split(/\r?\n/)) {
        const match = line.match(/^\{([\da-f-]{36})\}\s+(event:\/.*)$/i);
        if (match) names[match[1].toLowerCase()] = match[2].trim();
    }
    return names;
}
function bundleBanks(profile) {
    return profile.banks.map(bank => {
        if (!bank.startsWith('banks/') || bank.includes('..') || bank.includes('\\') || bank.includes(':')) throw new Error('工程资源路径无效。');
        return path.join(ROOT, 'VYgo', bank);
    });
}
function digest(file) {
    const stat = fs.statSync(file);
    if (!stat.isFile() || stat.size > 128 * 1024 * 1024) throw new Error('bank 不存在或超过 128 MiB 试听限制。');
    const signature = `${stat.size}:${stat.mtimeMs}`;
    if (hashes.get(file)?.signature !== signature) hashes.set(file, {
        signature, hash: crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex')
    });
    return hashes.get(file).hash;
}
function runtimeBanks(extra, profiles) {
    const shipped = path.join(ROOT, 'VYgo/banks/VYgo.bank');
    const candidates = [...originalBanks(), ...extra,
        ...(fs.existsSync(shipped) ? [shipped] : []), ...profiles.flatMap(bundleBanks)];
    const seen = new Set();
    const unique = candidates.filter(file => {
        const hash = digest(file);
        if (seen.has(hash)) return false;
        seen.add(hash); return true;
    });
    if (unique.reduce((bytes, file) => bytes + fs.statSync(file).size, 0) > 192 * 1024 * 1024)
        throw new Error('试听 bank 总大小超过 192 MiB，请拆分自定义工程。');
    return unique;
}
function inspectBanks(request) {
    runtimeInfo();
    if (inspections >= 2) return Promise.reject(new Error('正在检查其他工程，请稍后再试。'));
    inspections++;
    return new Promise((resolve, reject) => {
        const child = spawn(process.execPath, [path.resolve(__dirname, '../scripts/inspect-fmod-banks.js')], { windowsHide: true, stdio: ['pipe', 'pipe', 'pipe'] });
        let stdout = '', stderr = '';
        const timer = setTimeout(() => child.kill(), 30000);
        child.stdout.on('data', data => { stdout += data; });
        child.stderr.on('data', data => { stderr += data; });
        child.on('error', reject);
        child.on('close', code => {
            inspections--; clearTimeout(timer);
            try {
                const result = JSON.parse(stdout);
                if (code !== 0 || result.error) throw new Error(result.error || 'WASM 工程检查失败。');
                resolve(result);
            } catch (error) { reject(new Error(stdout ? error.message : `WASM 工程检查失败或超时：${stderr.slice(-400) || code}`)); }
        });
        child.stdin.on('error', () => {});
        child.stdin.end(JSON.stringify(request));
    });
}
function findTarget(data, profileId, event) {
    const profile = data.profiles.find(item => item.id === profileId);
    const target = profile?.events.find(item => item.path === event);
    if (!target) throw new Error('替换事件不存在，请重新选择已导入的工程事件。');
    return { profile, target };
}
async function createProfile(input, updateId) {
    if (typeof input.name !== 'string' || !input.name.trim()) throw new Error('请填写工程名称。');
    if (typeof input.bankDir !== 'string' || !input.bankDir.trim()) throw new Error('请填写已打包的 bank 目录。');
    if (!Array.isArray(input.bankFiles) || !input.bankFiles.length) throw new Error('请选择至少一个已打包的 bank。');
    const files = input.bankFiles.map(name => {
        if (typeof name !== 'string' || path.basename(name) !== name || !name.endsWith('.bank')) throw new Error('bank 文件名无效。');
        return path.resolve(input.bankDir, name);
    });
    if (new Set(files).size !== files.length) throw new Error('bank 文件重复。');
    const before = readJson(manifestPath);
    if (updateId && !before.profiles.some(profile => profile.id === updateId)) throw new Error('待更新工程不存在。');
    // Node 也使用同一份官方 WASM，只枚举事件，不生成音频或调用本机 DLL。
    const result = await inspectBanks({ banks: runtimeBanks(files, before.profiles.filter(profile => profile.id !== updateId)), guidNames: guidNames(input.guidFile) });
    const events = result.events.filter(item => files.includes(item.bank));
    if (!events.length) throw new Error('未找到可命名事件。请提供导出的 GUIDs.txt，或将工程 strings bank 一并导入。');
    if (events.some(item => allowed.has(item.path))) throw new Error('自定义事件必须使用独立路径，不能覆盖原版事件名称。');
    const data = readJson(manifestPath);
    const occupied = new Set(data.profiles.filter(profile => profile.id !== updateId).flatMap(profile => profile.events.map(event => event.path)));
    if (events.some(item => occupied.has(item.path))) throw new Error('事件路径与已导入工程重复，请使用独立事件路径。');
    if (Object.values(data.mappings).some(mapping => mapping.profileId === updateId && !events.some(event => event.path === mapping.event)))
        throw new Error('新 bank 缺少正在使用的替换事件，请先取消对应替换。');
    const id = updateId || crypto.randomUUID();
    const relativeDir = `banks/ui/${crypto.randomUUID()}`;
    const destination = path.join(ROOT, 'VYgo', relativeDir);
    fs.mkdirSync(destination, { recursive: true });
    try {
        const shipped = path.join(ROOT, 'VYgo/banks/VYgo.bank');
        const copiedBanks = files.map(file => {
            if (fs.existsSync(shipped) && digest(file) === digest(shipped)) return 'banks/VYgo.bank';
            fs.copyFileSync(file, path.join(destination, path.basename(file)));
            return `${relativeDir}/${path.basename(file)}`;
        });
        const eventList = events.map(({ path, guid }) => ({ path, guid }));
        fs.writeFileSync(path.join(destination, 'events.guids.txt'), eventList.map(item => `{${item.guid}} ${item.path}`).join('\n') + '\n');
        const profile = { id, name: input.name.trim(), banks: copiedBanks, guidFile: `${relativeDir}/events.guids.txt`, events: eventList };
        if (updateId) data.profiles[data.profiles.findIndex(item => item.id === updateId)] = profile;
        else data.profiles.push(profile);
        writeJson(manifestPath, data);
        return profile;
    } catch (error) { fs.rmSync(destination, { recursive: true, force: true }); throw error; }
}
function playback(input) {
    const data = readJson(manifestPath);
    let files = originalBanks(), guid;
    if (input.profileId) {
        const found = findTarget(data, input.profileId, input.event);
        if (/\/(music|bgm)\//i.test(found.target.path)) throw new Error('背景音乐不属于 UI 试听范围。');
        files = runtimeBanks(bundleBanks(found.profile), data.profiles);
        guid = found.target.guid;
    } else if (!allowed.has(input.event)) throw new Error('只允许试听清单内的原版 UI 事件。');
    const registry = runtimeBanks([], data.profiles);
    const revision = crypto.createHash('sha256').update(registry.map(digest).sort().join(':')).digest('hex');
    return { runtime: runtimeInfo(), revision, event: input.event, guid,
        banks: files.map(file => ({ id: digest(file), name: path.basename(file), bytes: fs.statSync(file).size, url: `/api/ui-audio/banks/${digest(file)}` })) };
}
function bankFile(id) {
    if (!/^[a-f0-9]{64}$/.test(id)) throw new Error('bank 标识无效。');
    const data = readJson(manifestPath);
    const file = runtimeBanks([], data.profiles).find(file => digest(file) === id);
    if (!file) throw new Error('bank 已变化或不存在，请刷新后重试。');
    return file;
}
function saveMapping(input) {
    if (!allowed.has(input.source)) throw new Error('该事件不在 UI 替换清单内。');
    const data = readJson(manifestPath);
    if (input.profileId === null) delete data.mappings[input.source];
    else {
        const { target } = findTarget(data, input.profileId, input.event);
        if (/\/(music|bgm)\//i.test(target.path)) throw new Error('背景音乐不属于此次 UI 音效替换范围。');
        data.mappings[input.source] = { profileId: input.profileId, event: target.path };
    }
    writeJson(manifestPath, data);
    return data.mappings;
}
module.exports = { state, saveSettings, createProfile, playback, bankFile, inspectBanks, saveMapping, allowed };
