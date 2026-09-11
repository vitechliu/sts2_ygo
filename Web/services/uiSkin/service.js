const fs = require('fs');
const path = require('path');
const sharp = require('sharp');
const { scan, local, originalImage, hash, vector } = require('./catalog');
const { ValidationError } = require('../inputValidation');
const { writeJsonAtomic } = require('../jsonFileService');

const STATES = ['normal', 'hover', 'pressed', 'disabled', 'selected', 'selected_hover', 'selected_pressed', 'selected_disabled'];
function check(condition, message) { if (!condition) throw new ValidationError(message); }
function ints(value, count, min, max, name) {
    check(Array.isArray(value) && value.length === count && value.every(n => Number.isInteger(n) && n >= min && n <= max), `${name}无效`);
    return value;
}
function sourceFile(base, id) {
    check(typeof id === 'string' && /^[a-f0-9]{64}\.png$/.test(id), '素材标识无效');
    return local(base, `res://sources/${id}`);
}

// 素材裁剪和适配在服务端统一实现，预览与导出使用同一结果。
async function render(base, spec, outputSize) {
    check(spec && typeof spec === 'object', '缺少素材配置');
    if (spec.blank === true) {
        check(!spec.source, '空白映射不能同时指定素材');
        const [width, height] = ints(outputSize || spec.outputSize, 2, 1, 4096, '输出尺寸');
        check(width * height <= 8 * 1024 * 1024, '输出图片过大');
        const png = await sharp({ create: { width, height, channels: 4, background: '#00000000' } }).png().toBuffer();
        return { png, width, height, mode: spec.mode || 'contain', border: spec.border || [0, 0, 0, 0] };
    }
    const file = sourceFile(base, spec.source);
    const meta = await sharp(file, { limitInputPixels: 32 * 1024 * 1024 }).metadata();
    const crop = spec.crop || [0, 0, meta.width, meta.height];
    ints(crop, 4, 0, 16384, '裁剪区域');
    const [x, y, cw, ch] = crop;
    check(cw > 0 && ch > 0 && x + cw <= meta.width && y + ch <= meta.height, '裁剪区域越界');
    const padding = ints(spec.padding || [0, 0, 0, 0], 4, 0, 4096, '透明留白');
    const [left, top, right, bottom] = padding;
    const input = await sharp(file).extract({ left: x, top: y, width: cw, height: ch })
        .extend({ left, top, right, bottom, background: '#00000000' }).png().toBuffer();
    const mode = spec.mode || 'contain';
    check(['contain', 'cover', 'stretch', 'nine', 'tile'].includes(mode), '不支持的适配方式');
    const border = ints(spec.border || [0, 0, 0, 0], 4, 0, 4096, '九宫格边界');
    const width = cw + left + right, height = ch + top + bottom;
    check(border[0] + border[2] < width && border[1] + border[3] < height, '九宫格边界重叠，中间区域必须大于零');
    if (['nine', 'tile'].includes(mode) && !outputSize) return { png: input, width, height, border, mode };
    const [ow, oh] = ints(outputSize || spec.outputSize, 2, 1, 4096, '输出尺寸');
    check(ow * oh <= 8 * 1024 * 1024, '输出图片过大');
    const canvas = () => sharp({ create: { width: ow, height: oh, channels: 4, background: '#00000000' } });
    let png;
    if (mode === 'nine') {
        check(ow >= border[0] + border[2] && oh >= border[1] + border[3], '预览尺寸小于固定边框');
        const sx = [0, border[0], width - border[2], width], sy = [0, border[1], height - border[3], height];
        const dx = [0, border[0], ow - border[2], ow], dy = [0, border[1], oh - border[3], oh];
        const pieces = [];
        for (let row = 0; row < 3; row++) for (let col = 0; col < 3; col++) {
            const sw = sx[col + 1] - sx[col], sh = sy[row + 1] - sy[row], dw = dx[col + 1] - dx[col], dh = dy[row + 1] - dy[row];
            if (sw && sh && dw && dh) pieces.push({ left: dx[col], top: dy[row], input: await sharp(input)
                .extract({ left: sx[col], top: sy[row], width: sw, height: sh }).resize(dw, dh, { fit: 'fill' }).png().toBuffer() });
        }
        png = await canvas().composite(pieces).png().toBuffer();
    } else if (mode === 'tile') {
        const tile = await sharp(input).extract({ left: 0, top: 0, width: Math.min(width, ow), height: Math.min(height, oh) }).png().toBuffer();
        png = await canvas().composite([{ input: tile, tile: true, gravity: 'northwest' }]).png().toBuffer();
    } else {
        const align = spec.align || [0.5, 0.5];
        check(Array.isArray(align) && align.length === 2 && align.every(v => Number.isFinite(v) && v >= 0 && v <= 1), '对齐需为 0 至 1');
        const offset = ints(spec.offset || [0, 0], 2, -4096, 4096, '内容偏移');
        if (mode === 'stretch') png = await sharp(input).resize(ow, oh, { fit: 'fill' }).png().toBuffer();
        else {
            const scale = mode === 'contain' ? Math.min(ow / width, oh / height) : Math.max(ow / width, oh / height);
            const rw = Math.max(1, Math.round(width * scale)), rh = Math.max(1, Math.round(height * scale));
            check(rw * rh <= 32 * 1024 * 1024, '缩放后的图片过大');
            const px = Math.round((ow - rw) * align[0]) + offset[0], py = Math.round((oh - rh) * align[1]) + offset[1];
            const ex = Math.max(0, -px), ey = Math.max(0, -py), ew = Math.min(rw - ex, ow - Math.max(px, 0)), eh = Math.min(rh - ey, oh - Math.max(py, 0));
            check(ew > 0 && eh > 0, '内容偏移后图片完全位于画布外');
            const resized = await sharp(input).resize(rw, rh, { fit: 'fill' }).png().toBuffer();
            const cropped = await sharp(resized).extract({ left: ex, top: ey, width: ew, height: eh }).png().toBuffer();
            png = await canvas().composite([{ input: cropped, left: Math.max(px, 0), top: Math.max(py, 0) }]).png().toBuffer();
        }
    }
    return { png, width: ow, height: oh, border, mode };
}

class UiSkinService {
    constructor({ projectRoot = path.resolve(__dirname, '../../..'), vanillaRoot } = {}) {
        this.projectRoot = projectRoot;
        this.base = path.join(projectRoot, 'Web/ui-skin');
        this.output = path.join(projectRoot, 'VYgo/ui_skin');
        this.root = vanillaRoot;
        this.catalog = null;
        this.busy = false;
    }
    read(name, fallback) {
        const file = path.join(this.base, name);
        return fs.existsSync(file) ? JSON.parse(fs.readFileSync(file, 'utf8')) : fallback;
    }
    async scan(root) {
        check(!this.busy, '正在扫描或导出，请稍候');
        this.busy = true;
        try {
            const candidate = root || this.root || process.env.STS2_VANILLA_ROOT || this.read('local.json', {}).vanillaRoot;
            check(typeof candidate === 'string' && candidate.length > 0, '请设置原版导出工程目录');
            const catalog = await scan(candidate);
            this.root = fs.realpathSync(candidate);
            this.catalog = catalog;
            for (const captured of this.read('captures.json', [])) {
                try { await this.capture(captured.record); }
                catch (error) { catalog.warnings.push(`拾取记录需重新确认：${captured.id} (${error.message})`); }
            }
            writeJsonAtomic(path.join(this.base, 'catalog.json'), catalog);
            writeJsonAtomic(path.join(this.base, 'local.json'), { vanillaRoot: this.root });
            return catalog;
        } finally { this.busy = false; }
    }
    getCatalog() {
        this.catalog ||= this.read('catalog.json', null);
        this.root ||= process.env.STS2_VANILLA_ROOT || this.read('local.json', {}).vanillaRoot;
        check(this.catalog && this.root, '请先扫描原版资源');
        return this.catalog;
    }
    config() { return this.read('skin.json', { schemaVersion: 1, rules: [] }); }
    async importImage(bytes, name) {
        check(Buffer.isBuffer(bytes) && bytes.length > 0 && bytes.length <= 20 * 1024 * 1024, 'PNG 文件大小需在 20 MB 以内');
        const m = await sharp(bytes, { limitInputPixels: 32 * 1024 * 1024 }).metadata();
        check(m.format === 'png' && (!m.pages || m.pages === 1), '仅支持静态 PNG');
        const id = `${hash(bytes)}.png`;
        fs.mkdirSync(path.join(this.base, 'sources'), { recursive: true });
        const file = sourceFile(this.base, id);
        if (!fs.existsSync(file)) fs.writeFileSync(file, bytes, { flag: 'wx' });
        const sources = this.read('sources.json', []);
        if (!sources.some(s => s.id === id)) {
            sources.push({ id, name: String(name || '素材').slice(0, 150), width: m.width, height: m.height });
            writeJsonAtomic(path.join(this.base, 'sources.json'), sources);
        }
        return sources.find(s => s.id === id);
    }
    resolveRule(rule) {
        const catalog = this.getCatalog();
        check(rule && typeof rule === 'object' && /^[a-zA-Z0-9_-]{1,80}$/.test(rule.id), '规则名称只能使用字母、数字、下划线或短横线');
        const component = catalog.components.find(c => c.id === rule.componentId);
        const slot = component?.slots.find(s => s.id === rule.slotId);
        check(slot, '组件或图层不存在，请重新选择');
        check(['component', 'scene'].includes(rule.scope), '替换范围无效');
        if (rule.scope === 'scene') check(slot.usages.some(u => u.scene === rule.scene && u.nodePath === rule.nodePath), '指定场景不是此图层的使用位置');
        check(rule.states && rule.states.normal, '必须提供正常状态图');
        check(Object.keys(rule.states).every(s => STATES.includes(s)), '存在未知状态');
        for (const spec of Object.values(rule.states)) if (['nine', 'tile'].includes(spec.mode))
            check(['TextureRect', 'NinePatchRect', 'ColorRect'].includes(slot.type) || slot.property.startsWith('theme_override_styles/'), '该节点暂不支持动态九宫格或平铺，请使用普通适配');
        const layout = rule.layout || {};
        for (const target of ['visual', 'hitbox']) if (layout[target]) {
            check(rule.scope === 'scene', '布局改动仅允许指定场景规则');
            const edit = layout[target];
            check(typeof edit.nodePath === 'string' && !edit.nodePath.includes('..') && !edit.nodePath.startsWith('/'), '布局节点必须是场景内相对路径');
            ints(edit.offsets, 4, -8192, 8192, '布局偏移');
        }
        if (layout.text) {
            check(rule.scope === 'scene', '文字区域调整仅允许指定场景规则');
            check(typeof layout.text.nodePath === 'string' && !layout.text.nodePath.includes('..') && !layout.text.nodePath.startsWith('/'), '文字节点路径无效');
            ints(layout.text.margins, 4, 0, 1024, '文字内边距');
        }
        check(!rule.neutralize || ['TextureRect', 'NinePatchRect', 'ColorRect'].includes(slot.type), '此图层不支持独立视觉层');
        return { component, slot, asset: catalog.assets.find(a => a.id === slot.assetId) };
    }
    async renderStates(rule, asset) {
        const rendered = {};
        // 空白状态沿用同图层素材的逻辑尺寸和九宫格边界，避免状态切换改变布局。
        for (const [state, spec] of Object.entries(rule.states)) {
            check(spec && typeof spec === 'object' && (spec.blank === undefined || typeof spec.blank === 'boolean'), '状态素材配置无效');
            if (!spec.blank) rendered[state] = await render(this.base, { ...spec, outputSize: [asset.width, asset.height] });
        }
        const template = Object.values(rendered)[0] || { width: asset.width, height: asset.height, mode: 'contain', border: [0, 0, 0, 0] };
        for (const [state, spec] of Object.entries(rule.states)) if (spec.blank) {
            check(!spec.source, '空白映射不能同时指定素材');
            rendered[state] = await render(this.base, { blank: true, outputSize: [template.width, template.height], mode: template.mode, border: template.border });
        }
        return rendered;
    }
    async validate(config) {
        check(config?.schemaVersion === 1 && Array.isArray(config.rules) && config.rules.length <= 512, '配置版本或规则数量无效');
        const conflicts = new Map(), ids = new Set(), layoutWrites = new Map();
        for (const rule of config.rules) {
            const { asset } = this.resolveRule(rule);
            check(!ids.has(rule.id), `规则名称重复：${rule.id}`); ids.add(rule.id);
            for (const [kind, edit] of Object.entries(rule.layout || {})) {
                if (!['visual', 'hitbox', 'text'].includes(kind)) throw new ValidationError('未知布局配置');
                const key = JSON.stringify([rule.scene, edit.nodePath, kind === 'text' ? 'margins' : 'offsets']);
                const value = JSON.stringify(kind === 'text' ? edit.margins : edit.offsets);
                check(!layoutWrites.has(key) || layoutWrites.get(key) === value, '多个规则修改同一节点的布局参数且值不一致');
                layoutWrites.set(key, value);
            }
            const { slot } = this.resolveRule(rule);
            const usages = rule.scope === 'scene' ? [{ scene: rule.scene, nodePath: rule.nodePath }] : slot.usages;
            for (const u of usages) {
                const key = JSON.stringify([rule.scope, u.scene, u.nodePath, slot.property]);
                const previous = conflicts.get(key) || [];
                check(previous.every(p => p.variant && slot.textureVariant && p.asset !== asset.id), `重复映射：${rule.id} 与已有规则覆盖同一节点`);
                previous.push({ variant: Boolean(slot.textureVariant), asset: asset.id }); conflicts.set(key, previous);
            }
            let size, mode, border;
            for (const image of Object.values(await this.renderStates(rule, asset))) {
                const actual = `${image.width},${image.height}`;
                check(!size || size === actual, '同一图层的所有状态图必须具有一致输出尺寸');
                check(!mode || mode === image.mode, '同一图层所有状态必须使用相同适配方式');
                check(!border || border === JSON.stringify(image.border), '同一图层所有状态必须使用相同九宫格边界');
                size = actual; mode = image.mode; border = JSON.stringify(image.border);
            }
        }
    }
    async save(config) {
        await this.validate(config);
        // 原版指纹随编辑配置保存，原版更新后必须显式核对并重新保存。
        const saved = { schemaVersion: 1, fingerprint: this.getCatalog().fingerprint, rules: config.rules };
        writeJsonAtomic(path.join(this.base, 'skin.json'), saved);
        return saved;
    }
    async preview(rule, state, size) {
        const { asset } = this.resolveRule(rule);
        const key = rule.states[state] ? state : state.startsWith('selected') && rule.states.selected ? 'selected' : 'normal';
        const spec = rule.states[key];
        if (spec.blank) {
            const image = (await this.renderStates(rule, asset))[key];
            return ['nine', 'tile'].includes(image.mode) && size ? render(this.base, { blank: true, mode: image.mode, border: image.border }, size) : image;
        }
        return render(this.base, { ...spec, outputSize: [asset.width, asset.height] }, ['nine', 'tile'].includes(spec.mode) ? size || [asset.width, asset.height] : undefined);
    }
    async export() {
        check(!this.busy, '正在扫描或导出，请稍候'); this.busy = true;
        try {
            const config = this.config(), catalog = this.getCatalog();
            const live = await scan(this.root);
            check(live.fingerprint === catalog.fingerprint, '原版文件已变化，请重新扫描并核对映射后导出');
            check(config.fingerprint === catalog.fingerprint || config.rules.length === 0, '原版资源已变化，请核对映射后重新保存');
            await this.validate(config);
            const rules = [], images = new Map();
            for (const rule of config.rules) {
                const { component, slot, asset } = this.resolveRule(rule);
                const states = {}; let result;
                for (const [state, image] of Object.entries(await this.renderStates(rule, asset))) {
                    result = image;
                    const name = `${hash(result.png)}.png`; images.set(name, result.png);
                    states[state] = `res://VYgo/ui_skin/generated/${name}`;
                }
                rules.push({ id: rule.id, componentId: component.id, componentScene: component.scene, adapter: component.adapter,
                    property: slot.property, nodePath: slot.nodePath,
                    selectors: (rule.scope === 'scene' ? [{ scene: rule.scene, nodePath: rule.nodePath }] : slot.usages),
                    priority: rule.scope === 'scene' ? 1 : 0, expectedType: slot.type, textureVariant: Boolean(slot.textureVariant),
                    texture: { path: asset.source === 'image' || !asset.id.includes('::') ? asset.id : '', atlas: asset.atlas || '', region: asset.region || [0, 0, 0, 0], margin: asset.margin || [0, 0, 0, 0], color: asset.color },
                    states, mode: result.mode, border: result.border, neutralize: Boolean(rule.neutralize), layout: rule.layout || {},
                    original: { region: vector(slot.props.region_rect, 4), border: ['left', 'top', 'right', 'bottom'].map(k => Number(slot.props[`patch_margin_${k}`] || 0)) } });
            }
            const manifest = { schemaVersion: 1, fingerprint: catalog.fingerprint, rules };
            // 内容寻址图片先落盘，最后原子替换清单；失败时旧皮肤仍可完整加载。
            fs.mkdirSync(path.join(this.output, 'generated'), { recursive: true });
            for (const [name, png] of images) {
                const target = path.join(this.output, 'generated', name);
                if (!fs.existsSync(target)) fs.writeFileSync(target, png, { flag: 'wx' });
            }
            writeJsonAtomic(path.join(this.output, 'skin.json'), manifest);
            return { rules: rules.length, images: images.size, fingerprint: manifest.fingerprint };
        } finally { this.busy = false; }
    }
    async original(id) { return originalImage(this.root, this.getCatalog().assets.find(a => a.id === id)); }
    async capture(record) {
        const catalog = this.getCatalog();
        check(record?.schemaVersion === 1 && Array.isArray(record.contexts) && record.contexts.length > 0 && record.textures, '无效的拾取记录');
        check(['TextureRect', 'NinePatchRect', 'TextureButton', 'Sprite2D', 'Control', 'Panel', 'PanelContainer', 'Button'].includes(record.type), '不支持的拾取节点类型');
        for (const context of record.contexts) {
            local(this.root, context.scene);
            check(typeof context.nodePath === 'string' && !context.nodePath.includes('..') && !context.nodePath.startsWith('/') && !context.nodePath.includes(':'), '拾取节点路径无效');
        }
        const first = record.contexts[0];
        const id = `capture-${hash(JSON.stringify(first)).slice(0, 12)}`;
        const slots = [];
        for (const [property, texture] of Object.entries(record.textures)) {
            check(property === 'texture' || ['texture_normal', 'texture_hover', 'texture_pressed', 'texture_disabled', 'texture_focused'].includes(property) || /^theme_override_styles\/[a-z_]+$/.test(property), '拾取属性无效');
            let asset = catalog.assets.find(a => texture.path && a.id === texture.path || texture.atlas && a.atlas === texture.atlas && JSON.stringify(a.region) === JSON.stringify(texture.region) && JSON.stringify(a.margin) === JSON.stringify(texture.margin));
            if (!asset) {
                if (texture.atlas) {
                    local(this.root, texture.atlas); ints(texture.region, 4, 0, 16384, '拾取裁剪区域'); ints(texture.margin, 4, 0, 16384, '拾取留白');
                    asset = { id: `${first.scene}::${id}-${property}`, name: `${id}-${property}`, source: 'atlas', atlas: texture.atlas, region: texture.region, margin: texture.margin,
                        width: texture.region[2] + texture.margin[2], height: texture.region[3] + texture.margin[3], refs: [] };
                } else {
                    const file = local(this.root, texture.path); const meta = await sharp(file).metadata();
                    asset = { id: texture.path, name: path.basename(texture.path), source: 'image', width: meta.width, height: meta.height, refs: [] };
                }
                await originalImage(this.root, asset);
                catalog.assets.push(asset);
            }
            slots.push({ id: `${first.nodePath}|${property}`, nodePath: first.nodePath, property, assetId: asset.id, type: record.type, props: record.props || {}, usages: record.contexts });
        }
        check(slots.length > 0, '拾取记录没有纹理');
        const component = { id, name: `拾取：${first.nodePath}`, category: '拾取节点', scene: first.scene, adapter: 'auto', slots, states: STATES };
        catalog.components = catalog.components.filter(c => c.id !== id); catalog.components.push(component);
        catalog.stats.components = catalog.components.length; catalog.stats.assets = catalog.assets.length;
        const captures = this.read('captures.json', []).filter(c => c.id !== id); captures.push({ id, record });
        writeJsonAtomic(path.join(this.base, 'captures.json'), captures);
        writeJsonAtomic(path.join(this.base, 'catalog.json'), catalog);
        return catalog;
    }
}
module.exports = { UiSkinService, render, sourceFile, STATES };
