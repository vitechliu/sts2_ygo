'use strict';
(() => {
    const $ = id => document.getElementById(id);
    const state = { catalog: null, config: { schemaVersion: 1, rules: [] }, sources: [], component: null, slot: null, draft: null, dirty: false, image: null, previewUrl: null, request: 0 };
    const labelNames = { normal: '正常', hover: '悬停', pressed: '按下', disabled: '禁用', selected: '选中', selected_hover: '选中 + 聚焦', selected_pressed: '选中 + 按下', selected_disabled: '选中 + 禁用' };
    const originalUrl = id => `/api/ui-skin/original?id=${encodeURIComponent(id)}`;
    function message(text, error = false) { $('message').textContent = text; $('message').classList.toggle('error', error); }
    async function api(route, method = 'GET', data) {
        const response = await fetch(`/api/ui-skin/${route}`, { method, headers: data === undefined ? {} : { 'Content-Type': 'application/json' }, body: data === undefined ? undefined : JSON.stringify(data) });
        if (!response.ok) throw new Error((await response.json()).error || response.statusText);
        return response.json();
    }
    async function run(fn) {
        document.body.classList.add('busy');
        try { await fn(); } catch (e) { message(e.message, true); }
        finally { document.body.classList.remove('busy'); }
    }
    function option(value, text) { const o = document.createElement('option'); o.value = value; o.textContent = text; return o; }
    function selectOptions(id, values, current) { $(id).replaceChildren(...values.map(([v, text]) => option(v, text))); if (current !== undefined) $(id).value = current; }
    function fields(id, names, prefix) {
        names.forEach((name, i) => {
            const label = document.createElement('label'); label.textContent = name;
            const input = document.createElement('input'); input.type = 'number'; input.id = `${prefix}-${i}`; input.value = '0';
            label.append(input); $(id).append(label);
        });
    }
    fields('crop-fields', ['X', 'Y', '宽', '高'], 'crop');
    fields('padding-fields', ['左', '上', '右', '下'], 'padding');
    fields('border-fields', ['左', '上', '右', '下'], 'border');
    for (const [name, title] of [['visual', '视觉区域'], ['hitbox', '点击区域'], ['text', '文字内边距']]) {
        const fieldset = document.createElement('fieldset');
        const legend = document.createElement('legend'); legend.textContent = title; fieldset.append(legend);
        const label = document.createElement('label'); label.textContent = name === 'text' ? 'MarginContainer 节点路径（空表示不修改）' : 'Control 节点路径（空表示不修改）';
        const input = document.createElement('input'); input.id = `layout-${name}-path`; label.append(input); fieldset.append(label);
        const grid = document.createElement('div'); grid.className = 'grid4'; grid.id = `layout-${name}`; fieldset.append(grid); $('layout-fields').append(fieldset);
        fields(grid.id, ['左', '上', '右', '下'], `layout-${name}`);
    }
    const readFour = prefix => [0, 1, 2, 3].map(i => Number($(`${prefix}-${i}`).value));
    const setFour = (prefix, values = [0, 0, 0, 0]) => values.forEach((v, i) => { $(`${prefix}-${i}`).value = v; });
    const clone = value => JSON.parse(JSON.stringify(value));
    function currentSpec() { return state.draft?.states[$('state').value]; }
    function ensureDraft() {
        if (!state.component || !state.slot) return;
        const suffix = state.slot.nodePath.split('/').slice(-2).join('-').toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '') || 'root';
        // 完整页面的节点路径很长，截断后附加图层序号，防止不同图层保存时相互覆盖。
        const id = `${`${state.component.id}-${suffix}`.slice(0, 70)}-${state.component.slots.indexOf(state.slot)}`;
        state.draft = { id, componentId: state.component.id, slotId: state.slot.id, scope: 'component', states: {}, layout: {} };
        state.dirty = false;
    }
    function readForm() {
        if (!state.draft) return;
        const d = state.draft;
        d.id = $('rule-id').value.trim(); d.scope = $('scope').value; d.neutralize = $('neutralize').checked;
        const usage = state.slot.usages[Number($('target').value)];
        if (d.scope === 'scene' && usage) { d.scene = usage.scene; d.nodePath = usage.nodePath; }
        else { delete d.scene; delete d.nodePath; }
        if ($('source').value) d.states[$('state').value] = {
            source: $('source').value, crop: readFour('crop'), padding: readFour('padding'), border: readFour('border'), mode: $('mode').value,
            align: [Number($('align-x').value), Number($('align-y').value)], offset: [Number($('offset-x').value), Number($('offset-y').value)]
        };
        else delete d.states[$('state').value];
        d.layout = {};
        for (const name of ['visual', 'hitbox', 'text']) {
            const nodePath = $(`layout-${name}-path`).value.trim();
            if (nodePath) d.layout[name] = { nodePath, [name === 'text' ? 'margins' : 'offsets']: readFour(`layout-${name}`) };
        }
        state.dirty = true;
    }
    function fillForm() {
        const d = state.draft;
        if (!d) return;
        $('rule-id').value = d.id; $('scope').value = d.scope; $('neutralize').checked = Boolean(d.neutralize);
        $('target-label').hidden = d.scope !== 'scene';
        selectOptions('target', state.slot.usages.map((u, i) => [String(i), `${u.scene.slice(6)} · ${u.nodePath}`]), String(Math.max(0, state.slot.usages.findIndex(u => u.scene === d.scene && u.nodePath === d.nodePath))));
        for (const name of ['visual', 'hitbox', 'text']) {
            $(`layout-${name}-path`).value = d.layout?.[name]?.nodePath || '';
            setFour(`layout-${name}`, d.layout?.[name]?.[name === 'text' ? 'margins' : 'offsets']);
        }
        fillSpec(); renderUsage();
    }
    function fillSpec() {
        const spec = currentSpec();
        $('source').value = spec?.source || ''; $('mode').value = spec?.mode || 'contain';
        setFour('crop', spec?.crop); setFour('padding', spec?.padding); setFour('border', spec?.border);
        $('align-x').value = spec?.align?.[0] ?? .5; $('align-y').value = spec?.align?.[1] ?? .5;
        $('offset-x').value = spec?.offset?.[0] || 0; $('offset-y').value = spec?.offset?.[1] || 0;
        $('preview-state').textContent = spec ? '独立状态图' : '未提供：基础图 / 原版反馈';
        loadImage();
    }
    function canLeave() { return !state.dirty || window.confirm('当前映射有未保存的修改，是否放弃这些修改？'); }
    function choose(component, slotId, rule) {
        state.component = component;
        if (!component) { message('组件不在当前目录，请重新扫描或导入拾取记录', true); return; }
        state.slot = component.slots.find(s => s.id === slotId) || component.slots.find(s => s.nodePath === 'Sidebar/Panel' || s.nodePath === 'ColorRect') || component.slots.find(s => s.nodePath === 'Image' || s.nodePath === 'Bg') || component.slots[0];
        if (!state.slot) { message('此组件没有可用纹理图层', true); return; }
        selectOptions('slot', component.slots.map(s => [s.id, `${s.nodePath} · ${s.property === 'color' ? '纯色背景' : s.property} · ${state.catalog.assets.find(a => a.id === s.assetId)?.name || ''}`]), state.slot.id);
        const asset = state.catalog.assets.find(a => a.id === state.slot.assetId);
        $('title').textContent = component.name; $('subtitle').textContent = component.scene;
        $('original').src = originalUrl(asset.id);
        $('original-size').textContent = `${asset.width} × ${asset.height}${asset.source === 'color' ? '（纯色预览画布，实际大小随节点布局）' : ''}`;
        $('preview-width').value = asset.width; $('preview-height').value = asset.height;
        $('props').textContent = JSON.stringify({ texture: asset, componentNode: state.slot.props }, null, 2);
        $('state').value = 'normal';
        if (rule) state.draft = clone(rule); else ensureDraft();
        state.dirty = false;
        $('replacement').removeAttribute('src');
        fillForm(); renderComponents(); renderGallery();
        if ($('composition').open) run(componentPreview);
        if (state.draft.states.normal) run(preview);
    }
    function renderGallery() {
        $('layer-gallery').replaceChildren(...state.component.slots.map(s => {
            const div = document.createElement('div'), img = document.createElement('img'), text = document.createElement('small');
            img.src = originalUrl(s.assetId); img.alt = s.nodePath; img.loading = 'lazy'; text.textContent = s.nodePath;
            div.append(img, text); return div;
        }));
    }
    function renderUsage() {
        if (!state.slot) return;
        const usages = $('scope').value === 'scene' ? [state.slot.usages[Number($('target').value)]].filter(Boolean) : state.slot.usages;
        $('usage-count').textContent = `（${usages.length} 处场景引用）`;
        $('usage').replaceChildren(...usages.map(u => {
            const div = document.createElement('div'); div.className = 'usage-row'; div.textContent = `${u.scene}\n${u.nodePath} → ${state.slot.property}`; return div;
        }));
    }
    function renderComponents() {
        if (!state.catalog) return;
        const query = $('search').value.toLowerCase();
        $('components').replaceChildren(...state.catalog.components.filter(c => (!query || `${c.name} ${c.scene}`.toLowerCase().includes(query)) && (!$('category').value || c.category === $('category').value)).map(c => {
            const button = document.createElement('button'); button.classList.toggle('active', c.id === state.component?.id);
            const name = document.createElement('span'); name.textContent = c.name;
            const small = document.createElement('small'); small.textContent = `${c.category} · ${c.slots.length} 图层 · ${state.config.rules.filter(r => r.componentId === c.id).length} 映射`;
            button.append(name, small); button.onclick = () => { if (canLeave()) choose(c); }; return button;
        }));
        $('rule-count').textContent = `（${state.config.rules.length}）`;
        $('rules').replaceChildren(...state.config.rules.filter(r => !query || r.id.toLowerCase().includes(query)).map(r => {
            const button = document.createElement('button'); button.textContent = `${r.id} · ${r.scope === 'scene' ? '局部' : '共用'}`;
            button.onclick = () => { if (canLeave()) choose(state.catalog.components.find(c => c.id === r.componentId), r.slotId, r); }; return button;
        }));
    }
    function renderAssets() {
        if (!state.catalog) return;
        const query = $('asset-search').value.toLowerCase();
        const matches = state.catalog.assets.filter(a => `${a.id} ${a.name}`.toLowerCase().includes(query));
        $('assets').replaceChildren(...matches.slice(0, 80).map(a => {
            const div = document.createElement('div'); div.className = 'asset';
            const img = document.createElement('img'); img.src = originalUrl(a.id); img.loading = 'lazy'; img.alt = '';
            const text = document.createElement('span'); text.textContent = `${a.name} · ${a.width}×${a.height} · ${a.refs.length} 处`;
            div.append(img, text); div.title = a.id;
            const refs = a.refs.filter(r => ['TextureRect', 'NinePatchRect', 'TextureButton', 'Sprite2D', 'Control', 'Panel', 'PanelContainer', 'Button'].includes(r.type));
            if (refs.length) {
                const choices = document.createElement('select'); choices.setAttribute('aria-label', '选择图片的使用位置');
                choices.append(...refs.map((r, i) => option(String(i), `${r.scene.slice(6)} · ${r.nodePath}`)));
                const button = document.createElement('button'); button.textContent = '为此位置建立映射';
                button.onclick = () => run(async () => {
                    if (!canLeave()) return;
                    const ref = refs[Number(choices.value)];
                    const identity = { path: a.id.includes('::') ? '' : a.id, atlas: a.atlas || '', region: a.region || [0, 0, 0, 0], margin: a.margin || [0, 0, 0, 0] };
                    state.catalog = await api('capture', 'POST', { schemaVersion: 1, type: ref.type, contexts: [{ scene: ref.scene, nodePath: ref.nodePath }], textures: { [ref.property]: identity } });
                    renderCatalog(); choose(state.catalog.components.at(-1)); message('已建立位置映射。只有这个场景中的对应节点会被识别。');
                });
                const row = document.createElement('div'); row.className = 'asset-actions'; row.append(choices, button); div.append(row);
            }
            return div;
        }));
    }
    function renderCatalog() {
        $('stats').textContent = `${state.catalog.stats.scenes} 场景 / ${state.catalog.stats.assets} 图片与背景 / ${state.catalog.stats.components} 组件`;
        selectOptions('category', [['', '全部类别'], ...[...new Set(state.catalog.components.map(c => c.category))].map(v => [v, v])]);
        $('warnings').textContent = state.catalog.warnings.join('\n') || '扫描无诊断信息';
        renderComponents(); renderAssets();
    }
    function renderSources() { selectOptions('source', [['', '未提供：沿用基础图与原版反馈'], ...state.sources.map(s => [s.id, `${s.name} (${s.width}×${s.height})`])], currentSpec()?.source || ''); }
    async function loadImage() {
        state.image = null;
        const id = $('source').value;
        if (!id) { drawCanvas(); return; }
        const img = new Image();
        img.onload = () => { if ($('source').value === id) { state.image = img; drawCanvas(); } };
        img.src = `/api/ui-skin/sources/${id}`;
    }
    function drawCanvas() {
        const canvas = $('source-canvas'), ctx = canvas.getContext('2d'), img = state.image;
        if (!img) { canvas.width = 420; canvas.height = 150; ctx.fillStyle = '#9badbe'; ctx.font = '14px sans-serif'; ctx.fillText('导入素材后可直接拖动编辑', 25, 75); return; }
        const crop = readFour('crop'), pad = readFour('padding'), border = readFour('border');
        const nine = $('canvas-mode').value === 'border';
        const width = nine ? crop[2] + pad[0] + pad[2] : img.width, height = nine ? crop[3] + pad[1] + pad[3] : img.height;
        if (width <= 0 || height <= 0) return;
        const scale = Math.min(1, 780 / width, 650 / height);
        canvas.width = Math.max(1, Math.round(width * scale)); canvas.height = Math.max(1, Math.round(height * scale)); canvas.dataset.scale = scale;
        ctx.scale(scale, scale);
        if (nine) ctx.drawImage(img, ...crop, pad[0], pad[1], crop[2], crop[3]); else ctx.drawImage(img, 0, 0);
        ctx.lineWidth = 2 / scale;
        if (nine) {
            ctx.strokeStyle = '#77dcc6';
            for (const x of [border[0], width - border[2]]) { ctx.beginPath(); ctx.moveTo(x, 0); ctx.lineTo(x, height); ctx.stroke(); }
            for (const y of [border[1], height - border[3]]) { ctx.beginPath(); ctx.moveTo(0, y); ctx.lineTo(width, y); ctx.stroke(); }
        } else { ctx.strokeStyle = '#77dcc6'; ctx.strokeRect(...crop); }
    }
    let drag;
    function point(e) { const c = $('source-canvas'), r = c.getBoundingClientRect(); return [(e.clientX - r.left) / r.width * c.width / Number(c.dataset.scale), (e.clientY - r.top) / r.height * c.height / Number(c.dataset.scale)].map(Math.round); }
    $('source-canvas').onpointerdown = e => {
        if (!state.image) return;
        const p = point(e), c = readFour('crop'), pad = readFour('padding'), b = readFour('border');
        const w = c[2] + pad[0] + pad[2], h = c[3] + pad[1] + pad[3];
        if ($('canvas-mode').value === 'crop') drag = { start: p };
        else { const distances = [Math.abs(p[0] - b[0]), Math.abs(p[1] - b[1]), Math.abs(p[0] - (w - b[2])), Math.abs(p[1] - (h - b[3]))]; drag = { edge: distances.indexOf(Math.min(...distances)), w, h }; }
        e.target.setPointerCapture(e.pointerId);
    };
    $('source-canvas').onpointermove = e => {
        if (!drag) return;
        const p = point(e);
        if (drag.start) {
            const x = Math.max(0, Math.min(drag.start[0], p[0], state.image.width - 1)), y = Math.max(0, Math.min(drag.start[1], p[1], state.image.height - 1));
            setFour('crop', [x, y, Math.max(1, Math.min(state.image.width, Math.max(drag.start[0], p[0])) - x), Math.max(1, Math.min(state.image.height, Math.max(drag.start[1], p[1])) - y)]);
        } else {
            const b = readFour('border'), e = drag.edge;
            const max = (e % 2 ? drag.h : drag.w) - b[(e + 2) % 4] - 1;
            b[e] = Math.max(0, Math.min(max, [p[0], p[1], drag.w - p[0], drag.h - p[1]][e])); setFour('border', b);
        }
        readForm(); drawCanvas();
    };
    $('source-canvas').onpointerup = () => { drag = null; };
    $('source-canvas').onpointercancel = () => { drag = null; };
    async function preview() {
        if (!state.draft) return;
        const request = ++state.request;
        const response = await fetch('/api/ui-skin/preview', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ rule: state.draft, state: $('state').value, size: [Number($('preview-width').value), Number($('preview-height').value)] }) });
        if (!response.ok) throw new Error((await response.json()).error);
        const blob = await response.blob();
        if (request !== state.request) return;
        if (state.previewUrl) URL.revokeObjectURL(state.previewUrl);
        state.previewUrl = URL.createObjectURL(blob); $('replacement').src = state.previewUrl;
        $('preview-state').textContent = state.draft.states[$('state').value] ? labelNames[$('state').value] : '基础图回退';
        if ($('composition').open) await componentPreview();
    }
    const loadPreviewImage = url => new Promise((resolve, reject) => { const img = new Image(); img.onload = () => resolve(img); img.onerror = () => reject(new Error('组合预览图片加载失败')); img.src = url; });
    const parseVector = value => (String(value || '').replace(/^\w+\(/, '').match(/-?\d+(?:\.\d+)?/g) || []).map(Number);
    let compositionRequest = 0;
    async function componentPreview() {
        if (!state.component) return;
        const request = ++compositionRequest, component = state.component, nodes = new Map((component.nodes || []).map(n => [n.path, n]));
        const rootProps = nodes.get('.')?.props || {};
        const rw = Math.abs(Number(rootProps.offset_right || 320) - Number(rootProps.offset_left || 0)) || 320;
        const rh = Math.abs(Number(rootProps.offset_bottom || 180) - Number(rootProps.offset_top || 0)) || 180;
        const rects = new Map([['.', [0, 0, rw, rh]]]);
        function rect(nodePath, visited = new Set()) {
            if (rects.has(nodePath)) return rects.get(nodePath);
            if (visited.has(nodePath)) return [0, 0, rw, rh]; visited.add(nodePath);
            const n = nodes.get(nodePath), p = n?.props || {}, parent = nodePath.includes('/') ? nodePath.slice(0, nodePath.lastIndexOf('/')) : '.';
            const pr = rect(parent, visited);
            let x = pr[0] + Number(p.anchor_left || 0) * pr[2] + Number(p.offset_left || 0), y = pr[1] + Number(p.anchor_top || 0) * pr[3] + Number(p.offset_top || 0);
            let w = Number(p.anchor_right || 0) * pr[2] + Number(p.offset_right || 0) - (x - pr[0]);
            let h = Number(p.anchor_bottom || 0) * pr[3] + Number(p.offset_bottom || 0) - (y - pr[1]);
            if (Number(p.layout_mode) === 2 || w <= 0 || h <= 0) { w = pr[2]; h = pr[3]; }
            const size = parseVector(p.scale); if (size.length === 2) { w *= size[0]; h *= size[1]; }
            const result = [x, y, w, h]; rects.set(nodePath, result); return result;
        }
        const entries = [];
        for (const slot of component.slots) {
            if (slot.props.visible === 'false') continue;
            const original = await loadPreviewImage(originalUrl(slot.assetId));
            const r = rect(slot.nodePath);
            let replacement = original, replacementMode, border;
            let rule = state.config.rules.find(r => r.componentId === component.id && r.slotId === slot.id && r.scope === 'component');
            if (state.draft?.slotId === slot.id && state.draft.states.normal) rule = state.draft;
            if (rule) {
                const response = await fetch('/api/ui-skin/preview', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ rule, state: $('state').value, size: [Math.max(1, Math.min(4096, Math.round(r[2]))), Math.max(1, Math.min(4096, Math.round(r[3])))] }) });
                if (!response.ok) throw new Error((await response.json()).error);
                const url = URL.createObjectURL(await response.blob());
                try { replacement = await loadPreviewImage(url); } finally { URL.revokeObjectURL(url); }
                const spec = rule.states[$('state').value] || rule.states.normal; replacementMode = spec.mode;
                border = spec.border;
            }
            entries.push({ slot, original, replacement, r, replacementMode, border });
        }
        if (request !== compositionRequest) return;
        const minX = Math.min(0, ...entries.map(e => e.r[0])), minY = Math.min(0, ...entries.map(e => e.r[1]));
        const maxX = Math.max(rw, ...entries.map(e => e.r[0] + e.r[2])), maxY = Math.max(rh, ...entries.map(e => e.r[1] + e.r[3]));
        for (const variant of ['before', 'after']) {
            const canvas = $('composition-' + variant), ctx = canvas.getContext('2d'); ctx.clearRect(0, 0, canvas.width, canvas.height);
            const scale = Math.min(460 / (maxX - minX), 260 / (maxY - minY)); ctx.save(); ctx.translate(20 - minX * scale, 20 - minY * scale); ctx.scale(scale, scale);
            for (const e of entries) {
                const img = variant === 'before' ? e.original : e.replacement, p = e.slot.props; let r = [...e.r];
                const color = parseVector(p.modulate); ctx.globalAlpha = color[3] ?? 1;
                // 阴影色在浏览器组合中用透明度近似；不宣称等同原版材质。
                if (Number(p.stretch_mode) === 5 && !(variant === 'after' && ['nine', 'tile'].includes(e.replacementMode))) {
                    const fit = Math.min(r[2] / img.width, r[3] / img.height), w = img.width * fit, h = img.height * fit;
                    r = [r[0] + (r[2] - w) / 2, r[1] + (r[3] - h) / 2, w, h];
                }
                ctx.drawImage(img, ...r);
                if (e.slot.id === state.slot.id) { ctx.globalAlpha = .7; ctx.strokeStyle = '#77dcc6'; ctx.lineWidth = 1 / scale; ctx.strokeRect(...r); }
            }
            ctx.restore(); ctx.globalAlpha = 1;
        }
    }
    $('composition').ontoggle = () => { if ($('composition').open) run(componentPreview); };
    $('scan').onclick = () => run(async () => {
        if (!canLeave()) return;
        message('正在扫描场景、图集和透明留白…');
        state.catalog = await api('scan', 'POST', { root: $('root').value || undefined });
        renderCatalog(); if (state.catalog.components.length) choose(state.catalog.components[0]);
        message(`已扫描 ${state.catalog.stats.scenes} 个场景，${state.catalog.stats.assets} 张图片。`);
    });
    $('slot').onchange = () => { if (canLeave()) choose(state.component, $('slot').value); else $('slot').value = state.slot.id; };
    $('state').onchange = () => { fillSpec(); if (state.draft?.states.normal) run(preview); };
    $('source').onchange = () => {
        const source = state.sources.find(s => s.id === $('source').value);
        if (source) setFour('crop', [0, 0, source.width, source.height]);
        readForm(); loadImage();
    };
    $('upload').onchange = () => run(async () => {
        const file = $('upload').files[0]; if (!file) return;
        const response = await fetch(`/api/ui-skin/sources?name=${encodeURIComponent(file.name)}`, { method: 'POST', headers: { 'Content-Type': 'image/png' }, body: file });
        const result = await response.json(); if (!response.ok) throw new Error(result.error);
        state.sources = await api('sources'); renderSources(); $('source').value = result.id;
        setFour('crop', [0, 0, result.width, result.height]); readForm(); loadImage(); message('原始素材已保存。可以框选裁剪区域，再调整适配方式。');
    });
    $('capture').onchange = () => run(async () => {
        const file = $('capture').files[0]; if (!file) return;
        const capture = JSON.parse(await file.text());
        state.catalog = await api('capture', 'POST', capture); renderCatalog(); choose(state.catalog.components.at(-1));
        message('已导入拾取位置，可为此节点建立替换映射。');
    });
    $('clear-state').onclick = () => { if (state.draft) { delete state.draft.states[$('state').value]; state.dirty = true; fillSpec(); } };
    $('reset-crop').onclick = () => { if (state.image) { setFour('crop', [0, 0, state.image.width, state.image.height]); readForm(); drawCanvas(); } };
    $('canvas-mode').onchange = drawCanvas;
    $('scope').onchange = () => { $('target-label').hidden = $('scope').value !== 'scene'; readForm(); renderUsage(); };
    $('target').onchange = () => { readForm(); renderUsage(); };
    for (const el of document.querySelectorAll('.editor-column input:not([type=file]), .editor-column select:not(#source):not(#scope):not(#target):not(#canvas-mode)')) el.addEventListener('change', () => { readForm(); drawCanvas(); });
    $('preview').onclick = () => run(async () => { readForm(); await preview(); });
    $('save').onclick = () => run(async () => {
        readForm(); if (!state.draft) return;
        const rules = state.config.rules.filter(r => r.id !== state.draft.id); rules.push(clone(state.draft));
        state.config = await api('config', 'PUT', { schemaVersion: 1, rules });
        state.dirty = false; renderComponents(); message(`已保存 ${state.draft.id}。导出后重新打包并重启游戏生效。`);
    });
    $('new-rule').onclick = () => { if (state.component && canLeave()) { ensureDraft(); fillForm(); } };
    $('delete').onclick = () => run(async () => {
        if (!state.draft?.id || !window.confirm(`删除映射 ${state.draft.id}？原始图片会保留。`)) return;
        state.config = await api('config', 'PUT', { schemaVersion: 1, rules: state.config.rules.filter(r => r.id !== state.draft.id) });
        ensureDraft(); fillForm(); renderComponents(); message('映射已删除；导出后重新打包生效。');
    });
    $('export').onclick = () => run(async () => {
        if (state.dirty) throw new Error('请先保存当前映射再导出，避免遗漏未保存的修改');
        const result = await api('export', 'POST'); message(`已导出 ${result.rules} 条规则、${result.images} 张派生图。下一步：Godot 导入并运行 publish，再重启游戏。`);
    });
    $('search').oninput = renderComponents; $('category').onchange = renderComponents; $('asset-search').oninput = renderAssets;
    window.addEventListener('beforeunload', e => { if (state.dirty) { e.preventDefault(); e.returnValue = ''; } });
    run(async () => {
        [state.config, state.sources] = await Promise.all([api('config'), api('sources')]); renderSources();
        try { state.catalog = await api('catalog'); renderCatalog(); if (state.catalog.components.length) choose(state.catalog.components[0]); }
        catch (e) { message(e.message); }
    });
})();
