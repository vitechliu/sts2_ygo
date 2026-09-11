// 在隔离的 Node 进程中运行官方 WASM，只检查 bank，不输出音频。
const fs = require('fs');
const { runtimeFile, config } = require('../services/fmodRuntime');

function guidText(value) {
    const hex = (number, length) => number.toString(16).padStart(length, '0');
    const tail = value.Data4.map(byte => hex(byte, 2)).join('');
    return `${hex(value.Data1, 8)}-${hex(value.Data2, 4)}-${hex(value.Data3, 4)}-${tail.slice(0, 4)}-${tail.slice(4)}`;
}
async function inspect(request) {
    const factory = require(runtimeFile('fmodstudio.js'));
    const fmod = await factory({
        wasmBinary: fs.readFileSync(runtimeFile('fmodstudio.wasm')), INITIAL_MEMORY: 256 * 1024 * 1024,
        print: () => {}, printErr: () => {}
    });
    const check = (result, action) => {
        if (result !== fmod.OK) throw new Error(`${action}失败：${fmod.ErrorString(result)}（${result}）。请检查 bank 版本、依赖与插件。`);
    };
    const out = {};
    check(fmod.Studio_System_Create(out), '创建 WASM 引擎');
    const system = out.val;
    try {
        check(system.getCoreSystem(out), '读取混音器');
        const core = out.val;
        check(core.getVersion(out, {}), '读取版本');
        const expected = parseInt(config.version.replace(/\./g, ''), 16);
        if (out.val !== expected) throw new Error('WASM 实际版本与固定配置不一致。');
        check(core.setOutput(fmod.OUTPUTTYPE_NOSOUND_NRT), '设置只读检查');
        check(system.initialize(128, fmod.STUDIO_INIT_SYNCHRONOUS_UPDATE, fmod.INIT_NORMAL, null), '初始化');
        const banks = [];
        for (const [index, filename] of request.banks.entries()) {
            const virtual = `bank-${index}.bank`;
            fmod.FS_createDataFile('/', virtual, fs.readFileSync(filename), true, false);
            check(system.loadBankFile('/' + virtual, fmod.STUDIO_LOAD_BANK_NORMAL, out), `加载 ${filename}`);
            banks.push({ bank: out.val, filename });
        }
        const events = [];
        for (const { bank, filename } of banks) {
            check(bank.getEventCount(out), '统计事件');
            const count = out.val;
            if (!count) continue;
            check(bank.getEventList(out, count, {}), '枚举事件');
            for (const event of out.val) {
                const value = {};
                check(event.getID(value), '读取事件 GUID');
                const guid = guidText(value.val);
                let name = request.guidNames?.[guid];
                if (!name && event.getPath(value, 2048, {}) === fmod.OK) name = value.val;
                if (name) events.push({ path: name, guid, bank: filename });
            }
        }
        return { version: config.version, events };
    } finally { system.release(); }
}
if (require.main === module) {
    let input = '';
    process.stdin.setEncoding('utf8');
    process.stdin.on('data', data => { input += data; });
    process.stdin.on('end', async () => {
        try { console.log(JSON.stringify(await inspect(JSON.parse(input)))); }
        catch (error) { console.log(JSON.stringify({ error: error.message })); process.exitCode = 1; }
    });
}
module.exports = { inspect, guidText };
