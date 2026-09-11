(() => {
    let libraryPromise;
    const cancelled = () => new DOMException('试听已取消。', 'AbortError');
    const hexGuid = text => {
        const [a, b, c, d, e] = text.split('-');
        return { Data1: parseInt(a, 16), Data2: parseInt(b, 16), Data3: parseInt(c, 16),
            Data4: (d + e).match(/../g).map(byte => parseInt(byte, 16)) };
    };
    async function responseJson(response) {
        let data;
        try { data = await response.json(); } catch { throw new Error(`音效服务返回异常（HTTP ${response.status}）。`); }
        if (!response.ok) throw new Error(data.error || '音效服务请求失败。');
        return data;
    }
    function loadLibrary(runtime) {
        if (window.FMODModule) return Promise.resolve(window.FMODModule);
        if (!libraryPromise) libraryPromise = new Promise((resolve, reject) => {
            const script = document.createElement('script');
            const file = runtime.files.find(file => file.name === 'fmodstudio.js');
            script.src = file.url;
            script.integrity = 'sha256-' + btoa(String.fromCharCode(...file.sha256.match(/../g).map(byte => parseInt(byte, 16))));
            const timer = setTimeout(() => fail(), 20000);
            const fail = () => {
                clearTimeout(timer); script.remove(); libraryPromise = null;
                reject(new Error('FMOD WASM 加载失败，请在 Web 目录运行 npm run prepare:fmod 后重试。'));
            };
            script.onerror = fail;
            script.onload = () => { clearTimeout(timer); window.FMODModule ? resolve(window.FMODModule) : fail(); };
            document.head.append(script);
        });
        return libraryPromise;
    }

    class FmodPreview {
        constructor(onStatus = () => {}) {
            this.onStatus = onStatus;
            this.generation = 0;
            this.queue = Promise.resolve();
            this.banks = new Map();
            this.volume = 1;
        }
        check(result, action) {
            if (result !== this.fmod.OK) throw new Error(`${action}失败：${this.fmod.ErrorString(result)}（${result}）。请检查 bank 版本、样本依赖、默认参数和插件。`);
        }
        checkCurrent(token) { if (token !== this.generation) throw cancelled(); }
        stop() {
            this.generation++;
            this.controller?.abort();
            this.controller = null;
            this.finish('stopped');
        }
        finish(state, error) {
            const active = this.active;
            if (!active) return;
            this.active = null;
            active.instance.stop(this.fmod.STUDIO_STOP_IMMEDIATE);
            active.instance.release();
            this.system.flushCommands();
            active.instance.delete?.();
            active.description.unloadSampleData();
            active.description.delete?.();
            const result = { state, event: active.event, elapsed: performance.now() - active.started };
            this.onStatus(error ? { ...result, error: error.message } : result);
            error ? active.reject(error) : active.resolve(result);
        }
        tick() {
            try {
                this.check(this.system.update(), '更新音频');
                if (!this.active) return;
                const out = {};
                this.check(this.active.instance.getPlaybackState(out), '读取播放状态');
                if (out.val === this.fmod.STUDIO_PLAYBACK_STOPPED) this.finish('ended');
                else if (performance.now() - this.active.started >= 8000) this.finish('limited');
            } catch (error) {
                this.finish('error', error);
                clearInterval(this.timer);
            }
        }
        async initialize(runtime, token, signal) {
            if (this.system) return;
            this.onStatus({ state: 'loading', message: '正在初始化 FMOD WASM…' });
            const factory = await loadLibrary(runtime);
            this.checkCurrent(token);
            const wasmFile = runtime.files.find(file => file.name === 'fmodstudio.wasm');
            const response = await fetch(wasmFile.url, { signal });
            if (!response.ok) {
                await responseJson(response);
                throw new Error('缺少 WASM 运行库，请运行 npm run prepare:fmod。');
            }
            const wasmBinary = await response.arrayBuffer();
            this.checkCurrent(token);
            this.fmod = await factory({ wasmBinary, INITIAL_MEMORY: 256 * 1024 * 1024,
                print: () => {}, printErr: text => console.warn(`FMOD：${text}`) });
            this.checkCurrent(token);
            const out = {};
            this.check(this.fmod.Studio_System_Create(out), '创建音频引擎');
            this.system = out.val;
            this.check(this.system.getCoreSystem(out), '读取音频混音器');
            this.core = out.val;
            this.check(this.core.getVersion(out, {}), '读取 WASM 版本');
            if (out.val !== parseInt(runtime.version.replace(/\./g, ''), 16)) throw new Error('WASM 版本不匹配，请运行 npm run prepare:fmod 修复依赖。');
            this.check(this.core.setDSPBufferSize(2048, 2), '设置音频缓冲');
            this.check(this.system.initialize(128, this.fmod.STUDIO_INIT_NORMAL, this.fmod.INIT_NORMAL, null), '初始化音频');
            this.timer = setInterval(() => this.tick(), 20);
        }
        async prepare(plan, token, signal) {
            if (this.revision && this.revision !== plan.revision) await this.destroy();
            await this.initialize(plan.runtime, token, signal);
            this.revision = plan.revision;
            for (const bank of plan.banks) {
                this.checkCurrent(token);
                if (this.banks.has(bank.id)) continue;
                this.onStatus({ state: 'loading', message: `正在加载 ${bank.name}（${(bank.bytes / 1048576).toFixed(1)} MiB）…` });
                const response = await fetch(bank.url, { signal });
                if (!response.ok) await responseJson(response);
                const bytes = new Uint8Array(await response.arrayBuffer());
                this.checkCurrent(token);
                const filename = bank.id + '.bank';
                this.fmod.FS_createDataFile('/', filename, bytes, true, false, true);
                const out = {};
                const result = this.system.loadBankFile('/' + filename, this.fmod.STUDIO_LOAD_BANK_NORMAL, out);
                if (result !== this.fmod.OK) {
                    this.fmod.FS_unlink('/' + filename);
                    this.check(result, `加载 ${bank.name}`);
                }
                this.banks.set(bank.id, { handle: out.val, filename });
            }
        }
        async unlock(token) {
            // 固定版运行库在首次点击后才异步加载。补调它安装的点击处理器，
            // 让 AudioWorklet 完成模块创建，而不要求用户再点击第二次。
            this.fmod.OutputAudioWorklet_resumeAudio?.();
            this.fmod.OutputWebAudio_resumeAudio?.();
            const context = this.fmod.mContext || this.fmod.context;
            if (context?.state === 'suspended') await context.resume();
            const deadline = performance.now() + 5000;
            while (this.fmod.mContext && !this.fmod.mWorkletNode) {
                this.checkCurrent(token);
                if (performance.now() > deadline) throw new Error('浏览器音频输出未能启动，请检查浏览器音频权限后重试。');
                await new Promise(resolve => setTimeout(resolve, 20));
            }
        }
        play(event, profileId) {
            this.stop();
            const token = this.generation;
            const controller = new AbortController();
            this.controller = controller;
            // 已有引擎时在点击回调内解锁 Web Audio；首次加载完成后再次检查。
            if (this.core) { this.core.mixerSuspend(); this.core.mixerResume(); }
            const operation = this.queue.catch(() => {}).then(async () => {
                let description, instance;
                const deadline = setTimeout(() => controller.abort(), 30000);
                try {
                    this.checkCurrent(token);
                    const plan = await responseJson(await fetch('/api/ui-audio/playback', {
                        method: 'POST', headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ event, profileId }), signal: controller.signal
                    }));
                    await this.prepare(plan, token, controller.signal);
                    this.checkCurrent(token);
                    this.check(this.core.mixerSuspend(), '挂起音频');
                    this.check(this.core.mixerResume(), '启动浏览器音频');
                    await this.unlock(token);
                    this.checkCurrent(token);
                    const out = {};
                    this.check(plan.guid ? this.system.getEventByID(hexGuid(plan.guid), out) : this.system.getEvent(event, out), '查找事件');
                    description = out.val;
                    this.check(description.loadSampleData(), '加载事件样本');
                    this.check(this.system.flushSampleLoading(), '等待样本');
                    this.check(description.createInstance(out), '创建试听');
                    instance = out.val;
                    this.check(instance.setVolume(this.volume), '设置试听音量');
                    this.check(instance.start(), '播放事件');
                    clearTimeout(deadline);
                    return await new Promise((resolve, reject) => {
                        this.active = { instance, description, event, started: performance.now(), resolve, reject };
                        instance = description = null;
                        this.onStatus({ state: 'playing', event });
                    });
                } catch (error) {
                    instance?.stop(this.fmod.STUDIO_STOP_IMMEDIATE);
                    instance?.release(); instance?.delete?.();
                    description?.unloadSampleData(); description?.delete?.();
                    if (token !== this.generation) {
                        // 初始化中被取消时也释放半成品引擎，避免下次初始化覆盖它。
                        await this.destroy();
                        return { state: 'cancelled', event };
                    }
                    await this.destroy();
                    if (error.name === 'AbortError') throw new Error('音效加载超时，请检查资源后重试。');
                    throw error;
                } finally { clearTimeout(deadline); }
            });
            this.queue = operation;
            return operation;
        }
        setVolume(volume) {
            this.volume = Math.max(0, Math.min(1, Number(volume)));
            this.active?.instance.setVolume(this.volume);
        }
        async destroy() {
            clearInterval(this.timer);
            this.finish('stopped');
            if (this.system) {
                for (const { handle, filename } of this.banks.values()) {
                    handle.unload(); handle.delete?.(); this.fmod.FS_unlink('/' + filename);
                }
                this.system.release(); this.system.delete?.(); this.core?.delete?.();
            }
            const context = this.fmod?.mContext || this.fmod?.context;
            if (context && context.state !== 'closed') await context.close();
            this.banks.clear();
            this.system = this.core = this.fmod = null;
            this.revision = null;
        }
        dispose() {
            this.stop();
            this.queue = this.queue.catch(() => {}).then(() => this.destroy());
            return this.queue;
        }
    }
    window.FmodPreview = FmodPreview;
})();
