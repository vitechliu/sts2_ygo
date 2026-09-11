const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { spawn } = require('child_process');

const ROOT = process.env.VYGO_UI_AUDIO_ROOT || path.resolve(__dirname, '../..');
const catalog = require('../../VYgo/audio/ui-events.json');
const allowed = new Set(catalog.map(item => item.path));
const manifestPath = path.join(ROOT, 'VYgo/audio/ui-replacements.json');
const settingsPath = path.join(ROOT, 'Web/audio-settings.local.json');
const cacheDir = path.join(ROOT, 'Web/.audio-cache');
const originalFiles = ['Master.bank', 'Master.strings.bank', 'sfx.bank'];
let activeProcesses = 0;

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
    return readJson(settingsPath, {
        dllDir: process.env.FMOD_DLL_DIR || '',
        originalBankDir: process.env.FMOD_ORIGINAL_BANK_DIR || '',
        python: process.env.FMOD_PYTHON || 'python'
    });
}
function originalBanks(config) {
    if (!config.dllDir || !config.originalBankDir) throw new Error('请先保存游戏目录和原版 bank 目录。');
    return originalFiles.map(name => path.join(config.originalBankDir, name));
}
function native(request, signal) {
    const config = settings();
    if (activeProcesses >= 2) return Promise.reject(new Error('试听服务正忙，请稍后再试。'));
    activeProcesses++;
    return new Promise((resolve, reject) => {
        const child = spawn(config.python, ['-X', 'utf8', path.join(ROOT, 'Web/scripts/fmod-preview.py')], {
            windowsHide: true, signal, env: { ...process.env, PYTHONIOENCODING: 'utf-8' }, stdio: ['pipe', 'pipe', 'pipe']
        });
        let stdout = '', stderr = '';
        const timer = setTimeout(() => child.kill(), 25000);
        child.stdout.on('data', data => { stdout += data; });
        child.stderr.on('data', data => { stderr += data; });
        child.on('error', error => { clearTimeout(timer); reject(new Error(`无法启动试听服务：${error.message}`)); });
        child.on('close', code => {
            activeProcesses--;
            clearTimeout(timer);
            try {
                const result = JSON.parse(stdout);
                if (code !== 0 || result.error) throw new Error(result.error || 'FMOD 处理失败。');
                resolve(result);
            } catch (error) {
                reject(new Error(stdout ? error.message : `FMOD 处理失败或超时：${stderr.slice(-600) || code}`));
            }
        });
        child.stdin.on('error', () => {});
        child.stdin.end(JSON.stringify({ dllDir: config.dllDir, ...request }));
    });
}
function state() {
    return { catalog, settings: settings(), ...readJson(manifestPath, { version: 1, profiles: [], mappings: {} }) };
}
function saveSettings(input) {
    const config = {};
    for (const key of ['dllDir', 'originalBankDir', 'python']) {
        if (typeof input[key] !== 'string' || !input[key].trim()) throw new Error('请填写完整试听环境。');
        config[key] = input[key].trim();
    }
    for (const file of ['fmod.dll', 'fmodstudio.dll']) {
        if (!fs.statSync(path.join(config.dllDir, file)).isFile()) throw new Error(`缺少 ${file}`);
    }
    originalBanks(config).forEach(file => { if (!fs.statSync(file).isFile()) throw new Error(`缺少原版 bank：${file}`); });
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
    return profile.banks.map(bank => path.join(ROOT, 'VYgo', bank));
}
function digest(file) {
    return crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
}
function runtimeBanks(extra, profiles) {
    const shipped = path.join(ROOT, 'VYgo/banks/VYgo.bank');
    const candidates = [...originalBanks(settings()), ...extra,
        ...(fs.existsSync(shipped) ? [shipped] : []), ...profiles.flatMap(bundleBanks)];
    const hashes = new Set();
    return candidates.filter(file => {
        const hash = digest(file);
        if (hashes.has(hash)) return false;
        hashes.add(hash);
        return true;
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
    if (!Array.isArray(input.bankFiles) || !input.bankFiles.length) throw new Error('请选择至少一个已打包的 bank。');
    const files = input.bankFiles.map(name => {
        if (typeof name !== 'string' || path.basename(name) !== name || !name.endsWith('.bank')) throw new Error('bank 文件名无效。');
        return path.resolve(input.bankDir, name);
    });
    if (new Set(files).size !== files.length) throw new Error('bank 文件重复。');
    const before = readJson(manifestPath);
    if (updateId && !before.profiles.some(profile => profile.id === updateId)) throw new Error('待更新工程不存在。');
    const names = guidNames(input.guidFile);
    // 与原版总线共同加载，导入时就检查版本和 bank 依赖。
    const result = await native({ banks: runtimeBanks(files, before.profiles.filter(profile => profile.id !== updateId)), guidNames: names });
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
            // 现有 VYgo 工程沿用入口注册的同一资源，避免重复加载同 GUID 的 bank。
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
    } catch (error) {
        fs.rmSync(destination, { recursive: true, force: true });
        throw error;
    }
}
async function preview(input, signal) {
    const data = readJson(manifestPath);
    let banks = originalBanks(settings()), guid;
    if (input.profileId) {
        const found = findTarget(data, input.profileId, input.event);
        banks = runtimeBanks(bundleBanks(found.profile), data.profiles);
        guid = found.target.guid;
    } else if (!allowed.has(input.event)) throw new Error('只允许试听清单内的原版 UI 事件。');
    fs.mkdirSync(cacheDir, { recursive: true });
    const output = path.join(cacheDir, `${crypto.randomUUID()}.wav`);
    try {
        const result = await native({ banks, guid, event: input.event, output }, signal);
        return { output, ...result };
    } catch (error) {
        fs.rmSync(output, { force: true });
        throw error;
    }
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
module.exports = { state, saveSettings, createProfile, preview, saveMapping, allowed };
