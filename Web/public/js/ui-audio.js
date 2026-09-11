(() => {
    let data, generation = 0;
    const drafts = new Map();
    const $ = id => document.getElementById(id);
    const player = new FmodPreview(status => {
        if (status.state === 'loading') message(status.message);
        if (status.state === 'playing') message(`正在试听：${status.event}`);
        if (status.state === 'ended') message('试听结束。');
        if (status.state === 'limited') message('已达到八秒试听上限。');
    });
    function message(text, error = false) {
        $('audio-message').textContent = text;
        $('audio-message').dataset.error = String(error);
    }
    async function api(endpoint, method = 'GET', body) {
        const response = await fetch(`/api/ui-audio${endpoint}`, {
            method, headers: { 'Content-Type': 'application/json' }, body: body ? JSON.stringify(body) : undefined
        });
        const result = await response.json();
        if (!response.ok) throw new Error(result.error);
        return result;
    }
    function stop() {
        generation++;
        player.stop();
    }
    async function preview(event, profileId) {
        stop();
        const current = generation;
        message('正在准备试听…');
        try {
            await player.play(event, profileId);
        } catch (error) {
            if (current === generation && error.name !== 'AbortError') message(error.message, true);
        }
    }
    function button(text, action) {
        const element = document.createElement('button');
        element.type = 'button';
        element.textContent = text;
        element.className = 'btn-secondary';
        element.addEventListener('click', async () => {
            element.disabled = true;
            try { await action(); } catch (error) { message(error.message, true); }
            finally { element.disabled = false; }
        });
        return element;
    }
    function render() {
        const container = $('audio-event-groups');
        const oldGroups = new Map([...container.querySelectorAll('details')].map(item => [item.dataset.group, item.open]));
        container.replaceChildren();
        for (const group of [...new Set(data.catalog.map(item => item.group))]) {
            const details = document.createElement('details');
            details.className = 'audio-group';
            details.dataset.group = group;
            details.open = oldGroups.get(group) ?? group === '通用按钮';
            const items = data.catalog.filter(item => item.group === group && (!$('audio-configured-only').checked || data.mappings[item.path]));
            const summary = document.createElement('summary');
            summary.textContent = `${group}（${items.length}）`;
            details.append(summary);
            for (const item of items) {
                const row = document.createElement('div');
                row.className = 'audio-event';
                const description = document.createElement('div');
                const title = document.createElement('strong');
                title.textContent = item.label;
                const code = document.createElement('code');
                code.textContent = item.path;
                const saved = document.createElement('span');
                saved.className = 'audio-saved';
                saved.textContent = data.mappings[item.path] ? '已配置' : '使用原版';
                description.append(title, code, saved);
                const select = document.createElement('select');
                select.setAttribute('aria-label', `${item.label}的替换事件`);
                select.add(new Option('选择工程中的事件', ''));
                for (const profile of data.profiles) {
                    const options = document.createElement('optgroup');
                    options.label = profile.name;
                    for (const event of profile.events) {
                        if (/\/(music|bgm)\//i.test(event.path)) continue;
                        options.append(new Option(event.path, JSON.stringify({ profileId: profile.id, event: event.path })));
                    }
                    select.append(options);
                }
                const mapping = drafts.has(item.path) ? drafts.get(item.path) : data.mappings[item.path];
                select.value = mapping ? JSON.stringify(mapping) : '';
                select.addEventListener('change', () => drafts.set(item.path, select.value ? JSON.parse(select.value) : null));
                const selected = () => {
                    if (!select.value) throw new Error('请先选择替换事件。');
                    return JSON.parse(select.value);
                };
                const actions = document.createElement('div');
                actions.className = 'audio-event-actions';
                actions.append(
                    button('原版试听', () => preview(item.path)),
                    button('替换后试听', () => { const target = selected(); return preview(target.event, target.profileId); }),
                    button('保存', async () => {
                        data.mappings = await api('/mapping', 'PUT', { source: item.path, ...selected() });
                        drafts.delete(item.path); render(); message('替换已保存。重新发布 Mod 并重启游戏后生效。');
                    }),
                    button('撤销编辑', () => { drafts.delete(item.path); render(); }),
                    button('取消替换', async () => {
                        data.mappings = await api('/mapping', 'PUT', { source: item.path, profileId: null });
                        drafts.delete(item.path); render(); message('已取消替换，重新发布并重启游戏后恢复原版。');
                    })
                );
                row.append(description, select, actions);
                details.append(row);
            }
            container.append(details);
        }
    }
    async function load() {
        try {
            data = await api('');
            const choice = $('audio-profile-choice');
            const selectedProfile = choice.value;
            choice.replaceChildren(new Option('新建工程', ''));
            data.profiles.forEach(profile => choice.add(new Option(profile.name, profile.id)));
            choice.value = selectedProfile;
            for (const [key, value] of Object.entries(data.settings)) $('audio-settings-form').elements[key].value = value;
            if (!data.settings.originalBankDir) $('audio-settings-form').closest('details').open = true;
            render();
        } catch (error) { message(error.message, true); }
    }
    $('audio-settings-form').addEventListener('submit', async event => {
        event.preventDefault();
        try {
            await api('/settings', 'PUT', Object.fromEntries(new FormData(event.target)));
            stop(); await player.dispose();
            message('试听环境已保存。');
        } catch (error) { message(error.message, true); }
    });
    $('audio-profile-form').addEventListener('submit', async event => {
        event.preventDefault();
        const submit = event.target.querySelector('button');
        submit.disabled = true;
        try {
            const input = Object.fromEntries(new FormData(event.target));
            input.bankFiles = input.bankFiles.split(/[,，]/).map(name => name.trim()).filter(Boolean);
            stop(); await player.dispose();
            message('正在使用 WASM 检查并导入 bank…');
            const profile = await api(input.profileId ? `/profiles/${input.profileId}` : '/profiles', input.profileId ? 'PUT' : 'POST', input);
            await load(); message(`已保存工程「${profile.name}」，包含 ${profile.events.length} 个事件。`);
        } catch (error) { message(error.message, true); }
        finally { submit.disabled = false; }
    });
    $('audio-stop').addEventListener('click', () => { stop(); message('已停止试听。'); });
    $('audio-volume').addEventListener('input', event => player.setVolume(event.target.value));
    $('audio-profile-choice').addEventListener('change', event => {
        const profile = data.profiles.find(profile => profile.id === event.target.value);
        $('audio-profile-form').elements.name.value = profile?.name || '';
    });
    $('audio-refresh').addEventListener('click', load);
    $('audio-configured-only').addEventListener('change', () => data && render());
    document.querySelectorAll('.nav-btn').forEach(button => button.addEventListener('click', () => {
        if (button.dataset.tab === 'audio') {
            history.replaceState(null, '', '#audio');
            if (!data) load();
        } else {
            stop();
            player.dispose();
            if (location.hash === '#audio') history.replaceState(null, '', location.pathname + location.search);
        }
    }));
    document.addEventListener('DOMContentLoaded', () => {
        if (location.hash === '#audio') document.querySelector('[data-tab="audio"]').click();
    });
    window.addEventListener('pagehide', () => { stop(); player.dispose(); });
})();
