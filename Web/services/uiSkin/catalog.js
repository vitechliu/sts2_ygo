const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const sharp = require('sharp');
const { ValidationError } = require('../inputValidation');

// 此处列出经过原版场景与状态脚本核对的组件，不按贴图文件名推断状态。
const DEFINITIONS = [
    ['confirm', '确认按钮', '按钮', 'ui/confirm_button', 'button'],
    ['back', '返回按钮', '按钮', 'ui/back_button', 'button'],
    ['settings-popup', '设置弹窗', '面板', 'ui/settings_screen_popup', 'static'],
    ['hover-tip', '悬浮提示', '面板', 'ui/hover_tip', 'static'],
    ['scrollbar', '滚动条', '滚动条', 'ui/scrollbar', 'scrollbar'],
    ['dropdown', '下拉菜单', '滚动条', 'screens/settings_dropdown', 'button'],
    ['tickbox', '复选框', '复选框', 'screens/settings_tickbox', 'tickbox'],
    ['tab', '设置页签', '页签', 'screens/settings_tab', 'tab'],
    ['proceed', '继续按钮', '按钮', 'ui/proceed_button', 'button'],
    ['popup', '通用弹窗', '面板', 'ui/vertical_popup', 'static'],
    ['slider', '音量滑块', '滚动条', 'ui/volume_slider', 'scrollbar'],
    ['compendium', '百科大全入口与插图', '百科大全', 'screens/compendium_submenu', 'button'],
    ['submenu-short', '百科入口面板', '百科大全', 'screens/main_menu/submenu_button_short', 'button'],
    ['compendium-bottom', '百科底部按钮', '百科大全', 'screens/main_menu/compendium_bottom_button', 'button'],
    ['card-library', '卡牌图鉴背景与筛选区', '卡牌图鉴', 'screens/card_library/card_library', 'static'],
    ['library-pool', '卡牌图鉴角色筛选', '卡牌图鉴', 'screens/card_library/library_pool_toggle', 'pool'],
    ['library-sort', '卡牌图鉴排序按钮', '卡牌图鉴', 'screens/card_library/library_sort_button', 'sort'],
    ['library-type', '卡牌图鉴类型筛选', '卡牌图鉴', 'screens/card_library/card_type_tickbox', 'card-type'],
    ['library-tickbox', '卡牌图鉴复选框', '卡牌图鉴', 'screens/card_library/card_library_tickbox', 'tickbox'],
    ['library-rarity', '卡牌图鉴稀有度筛选', '卡牌图鉴', 'screens/card_library/rarity_tickbox', 'tickbox'],
    ['library-cost', '卡牌图鉴费用筛选', '卡牌图鉴', 'screens/card_library/card_cost_tickbox', 'tickbox'],
    ['library-stats', '卡牌图鉴统计面板', '卡牌图鉴', 'screens/card_library/card_library_stats', 'static'],
    ['stats-screen', '角色统计页面与页签', '角色统计', 'screens/stats_screen/stats_screen', 'static'],
    ['character-stats', '角色统计背景', '角色统计', 'screens/stats_screen/character_stats', 'static'],
    ['stats-section', '统计条目与聚焦框', '角色统计', 'screens/stats_screen/stats_screen_section', 'button'],
    ['share', '统计分享按钮', '角色统计', 'ui/share_button', 'button']
].map(([id, name, category, scene, adapter]) => ({ id, name, category, scene: `res://scenes/${scene}.tscn`, adapter }));

function walk(dir, suffix) {
    if (!fs.existsSync(dir)) return [];
    return fs.readdirSync(dir, { withFileTypes: true }).sort((a, b) => a.name.localeCompare(b.name))
        .flatMap(e => e.isDirectory() ? walk(path.join(dir, e.name), suffix) : e.isFile() && e.name.endsWith(suffix) ? [path.join(dir, e.name)] : []);
}
function local(root, resource) {
    if (typeof resource !== 'string' || !resource.startsWith('res://')) throw new ValidationError('无效资源路径');
    const relative = resource.slice(6);
    if (relative.includes('..') || relative.includes('\\') || path.isAbsolute(relative)) throw new ValidationError('资源路径越界');
    const file = path.join(root, relative);
    if (fs.existsSync(file)) {
        const rel = path.relative(fs.realpathSync(root), fs.realpathSync(file));
        if (rel.startsWith('..') || path.isAbsolute(rel)) throw new ValidationError('符号链接越界');
    }
    return file;
}
const hash = value => crypto.createHash('sha256').update(value).digest('hex');
const numbers = value => [...String(value || '').matchAll(/-?\d+(?:\.\d+)?/g)].map(m => Number(m[0]));
function vector(value, count) {
    const result = numbers(String(value || '').replace(/^\w+\(/, ''));
    return result.length === count ? result : Array(count).fill(0);
}
function sections(text) {
    const result = [];
    let current;
    for (const line of text.split(/\r?\n/)) {
        if (/^\[\w/.test(line)) {
            const attrs = Object.fromEntries([...line.matchAll(/(\w+)=("(?:\\.|[^"\\])*"|(?:Ext|Sub)Resource\("[^"]+"\))/g)]
                .map(m => [m[1], m[2].startsWith('"') ? JSON.parse(m[2]) : m[2]]));
            current = { kind: line.slice(1).split(/[ \]]/)[0], attrs, props: {} };
            result.push(current);
        } else if (current) {
            const m = line.match(/^([^=]+?)\s*=\s*(.+)$/);
            if (m) current.props[m[1].trim()] = m[2];
        }
    }
    return result;
}

async function scan(root) {
    root = fs.realpathSync(root);
    if (!fs.existsSync(path.join(root, 'scenes'))) throw new ValidationError('请选择包含 scenes 与 images 的原版导出工程');
    const assets = new Map(), documents = new Map(), warnings = [], fingerprints = [['catalog-version', '2']];
    const read = resource => {
        if (documents.has(resource)) return documents.get(resource);
        const file = local(root, resource);
        if (!fs.existsSync(file)) return [];
        const text = fs.readFileSync(file, 'utf8');
        fingerprints.push([resource, hash(text)]);
        const doc = sections(text);
        documents.set(resource, doc);
        return doc;
    };
    for (const file of walk(path.join(root, 'images/atlases'), '.tpsheet')) {
        const atlas = path.basename(file, '.tpsheet');
        const bytes = fs.readFileSync(file);
        fingerprints.push([`res://images/atlases/${atlas}.tpsheet`, hash(bytes)]);
        const data = JSON.parse(bytes);
        for (const page of data.textures) {
            const png = `res://images/atlases/${page.image}`;
            fingerprints.push([png, hash(fs.readFileSync(local(root, png)))]);
            for (const sprite of page.sprites) {
                const id = `res://images/atlases/${atlas}.sprites/${sprite.filename.replace(/\.png$/i, '')}.tres`;
                const r = sprite.region, m = sprite.margin || {};
                assets.set(id, { id, name: sprite.filename, atlas: png, region: [r.x, r.y, r.w, r.h],
                    margin: [m.x || 0, m.y || 0, m.w || 0, m.h || 0], width: r.w + (m.w || 0), height: r.h + (m.h || 0), source: 'atlas', refs: [] });
            }
        }
    }
    const texture = async (resource, ref, visiting = new Set()) => {
        const token = `${resource}::${ref}`;
        if (visiting.has(token)) return null;
        visiting = new Set([...visiting, token]);
        const m = String(ref).match(/^(Ext|Sub)Resource\("([^"]+)"\)$/);
        if (!m) return null;
        const section = read(resource).find(s => s.kind === (m[1] === 'Ext' ? 'ext_resource' : 'sub_resource') && s.attrs.id === m[2]);
        if (!section) return null;
        let id, props;
        if (m[1] === 'Ext') {
            id = section.attrs.path;
            if (!id) return null;
            if (assets.has(id)) return assets.get(id);
            if (id.endsWith('.tres')) props = read(id).find(s => s.kind === 'resource')?.props;
            else if (/\.png$/i.test(id)) {
                try {
                    const file = local(root, id), metadata = await sharp(file).metadata();
                    const entry = { id, name: path.basename(id), source: 'image', width: metadata.width, height: metadata.height, refs: [] };
                    assets.set(id, entry);
                    fingerprints.push([id, hash(fs.readFileSync(file))]);
                    return entry;
                } catch { warnings.push(`图片不可读：${id}`); return null; }
            }
        } else { id = `${resource}::${m[2]}`; props = section.props; }
        // StyleBoxTexture 的纹理与边距由使用它的控件本地覆盖，不能改共享 Theme。
        if (props?.texture && !props.atlas) return texture(m[1] === 'Ext' ? id : resource, props.texture, visiting);
        if (!props?.atlas) return null;
        if (assets.has(id)) return assets.get(id);
        const parent = await texture(m[1] === 'Ext' ? id : resource, props.atlas, visiting);
        if (!parent || parent.source !== 'image') { warnings.push(`未支持的嵌套 Atlas：${id}`); return null; }
        const region = vector(props.region, 4), margin = vector(props.margin, 4);
        if (!region[2]) region[2] = parent.width;
        if (!region[3]) region[3] = parent.height;
        const entry = { id, name: id.split('/').pop(), source: 'atlas', atlas: parent.id, region, margin,
            width: region[2] + margin[2], height: region[3] + margin[3], refs: [] };
        assets.set(id, entry);
        return entry;
    };
    const resolved = new Map();
    const resolveScene = async (scene, visiting = new Set()) => {
        if (resolved.has(scene)) return resolved.get(scene);
        if (visiting.has(scene)) { warnings.push(`循环场景：${scene}`); return []; }
        const nodes = new Map(), doc = read(scene);
        for (const section of doc.filter(s => s.kind === 'node')) {
            const a = section.attrs, nodePath = a.parent === undefined ? '.' : a.parent === '.' ? a.name : `${a.parent}/${a.name}`;
            if (a.instance) {
                const match = a.instance.match(/ExtResource\("([^"]+)"\)/);
                const target = doc.find(s => s.kind === 'ext_resource' && s.attrs.id === match?.[1])?.attrs.path;
                if (target) for (const child of await resolveScene(target, new Set([...visiting, scene]))) {
                    const full = nodePath === '.' ? child.path : child.path === '.' ? nodePath : `${nodePath}/${child.path}`;
                    nodes.set(full, { ...child, path: full, props: { ...child.props }, bindings: { ...child.bindings }, contexts: [...child.contexts] });
                }
            }
            const node = nodes.get(nodePath) || { path: nodePath, type: a.type, props: {}, bindings: {}, contexts: [] };
            node.type = a.type || node.type;
            Object.assign(node.props, section.props);
            // 引用按声明它的场景解析，避免实例覆盖中的局部资源 ID 与父场景混淆。
            for (const [property, value] of Object.entries(section.props)) {
                if (property === 'texture' || property.startsWith('texture_') || property.startsWith('theme_override_styles/')) {
                    const asset = await texture(scene, value);
                    if (asset) node.bindings[property] = asset.id;
                    else if (value === 'null') delete node.bindings[property];
                    else if (property.startsWith('theme_override_styles/')) warnings.push(`样式资源需运行时拾取确认：${scene} ${nodePath} ${property}`);
                }
            }
            nodes.set(nodePath, node);
        }
        const list = [...nodes.values()].map(n => ({ ...n, contexts: [...n.contexts, { scene, nodePath: n.path }] }));
        resolved.set(scene, list);
        return list;
    };
    for (const file of walk(path.join(root, 'scenes'), '.tscn')) {
        const scene = `res://${path.relative(root, file).split(path.sep).join('/')}`;
        await resolveScene(scene);
    }

    // 纯色面板也允许导入图片；以场景和节点标识，避免与真正的纹理路径混淆。
    for (const [scene, nodes] of resolved) if (/^res:\/\/scenes\/(screens|ui)\//.test(scene)) for (const n of nodes) {
        if (n.type !== 'ColorRect') continue;
        const color = n.props.color ? vector(n.props.color, 4) : [1, 1, 1, 1];
        const minimum = vector(n.props.custom_minimum_size, 2);
        const width = Math.round(Math.max(minimum[0], Number(n.props.offset_right || 0) - Number(n.props.offset_left || 0))) || 256;
        const height = Math.round(Math.max(minimum[1], Number(n.props.offset_bottom || 0) - Number(n.props.offset_top || 0))) || 128;
        const id = `${scene}::color:${n.path}`;
        assets.set(id, { id, name: `${n.path}（纯色背景）`, source: 'color', color, width: Math.min(4096, width), height: Math.min(4096, height), refs: [] });
        n.bindings.color = id;
    }

    // 经 NCompendiumSubmenu / NShortSubmenuButton 核对的运行时图标赋值。
    // 场景里的占位图并不一定等于进入页面后看到的图片。
    const compendium = 'res://scenes/screens/compendium_submenu.tscn';
    for (const [button, key] of [['CardLibraryButton', 'card_library'], ['RelicCollectionButton', 'relic_collection'], ['PotionLabButton', 'potion_lab'], ['BestiaryButton', 'bestiary']]) {
        const node = resolved.get(compendium)?.find(n => n.path.endsWith(`/${button}/Icon`));
        const id = `res://images/packed/main_menu/submenu_icon_compendium_${key}.png`;
        if (!node || !fs.existsSync(local(root, id))) continue;
        const bytes = fs.readFileSync(local(root, id)), metadata = await sharp(bytes).metadata();
        assets.set(id, { id, name: path.basename(id), source: 'image', width: metadata.width, height: metadata.height, refs: [] });
        fingerprints.push([id, hash(bytes)]);
        node.bindings.texture = id;
        node.dynamic = true;
    }
    for (const [scene, nodes] of resolved) for (const n of nodes) for (const [property, id] of Object.entries(n.bindings)) {
        assets.get(id)?.refs.push({ scene, nodePath: n.path, property, type: n.type });
    }
    // 按真实实例上下文建立索引，扩展整个 UI 目录时不反复遍历所有场景。
    const usageIndex = new Map();
    for (const [scene, nodes] of resolved) for (const n of nodes) for (const [property, id] of Object.entries(n.bindings)) for (const context of n.contexts) {
        const key = JSON.stringify([context.scene, context.nodePath, property, id]);
        if (!usageIndex.has(key)) usageIndex.set(key, []);
        usageIndex.get(key).push({ scene, nodePath: n.path });
    }
    const definitions = [...DEFINITIONS];
    // 未人工命名的界面仍可直接编辑全部图层，不再藏在原版图片搜索列表中。
    for (const scene of resolved.keys()) if (/^res:\/\/scenes\/(screens|ui)\//.test(scene) && !definitions.some(d => d.scene === scene)) {
        definitions.push({ id: `scene-${hash(scene).slice(0, 16)}`, name: path.basename(scene, '.tscn'), category: '其他界面', scene, adapter: 'auto' });
    }
    const components = [];
    for (const definition of definitions) {
        const nodes = resolved.get(definition.scene);
        if (!nodes) { warnings.push(`组件场景缺失：${definition.scene}`); continue; }
        const slots = [];
        for (const n of nodes) for (const [property, assetId] of Object.entries(n.bindings)) {
            if (!['TextureRect', 'NinePatchRect', 'TextureButton', 'Sprite2D', 'ColorRect'].includes(n.type) && !property.startsWith('theme_override_styles/')) continue;
            const usages = usageIndex.get(JSON.stringify([definition.scene, n.path, property, assetId])) || [];
            slots.push({ id: `${n.path}|${property}`, nodePath: n.path, property, assetId, type: n.type, props: n.props, usages });
        }
        if (!slots.length) continue;
        components.push({ ...definition, slots, nodes: nodes.map(n => ({ path: n.path, type: n.type, props: n.props })), states: ['normal', 'hover', 'pressed', 'disabled', 'selected', 'selected_hover', 'selected_pressed', 'selected_disabled'] });
    }
    // NGeneralStatsGrid / NCharacterStats 通过 NStatEntry.Create(imgUrl) 创建这些条目。
    // 同一节点路径承载不同图标，按原始纹理区分规则，不能把全部统计项一起换掉。
    const statsScene = 'res://scenes/screens/stats_screen/stats_screen_section.tscn';
    const iconNode = resolved.get(statsScene)?.find(n => n.path === 'HBoxContainer/Icon');
    if (iconNode) for (const [name, label] of [['achievements', '成就'], ['clock', '游戏时间'], ['cards', '卡牌'], ['swords', '胜负'], ['monsters', '怪物'], ['chest', '遗物'], ['potions_seen', '药水'], ['questionmark', '事件'], ['chain', '连胜']]) {
        const assetId = `res://images/atlases/stats_screen_atlas.sprites/stats_${name}.tres`;
        const asset = assets.get(assetId);
        if (!asset) continue;
        const usage = { scene: statsScene, nodePath: iconNode.path };
        asset.refs.push({ ...usage, property: 'texture', type: iconNode.type, dynamic: true });
        components.push({ id: `stats-icon-${name}`, name: `统计图标：${label}`, category: '角色统计', scene: statsScene, adapter: 'button',
            slots: [{ id: `${iconNode.path}|texture`, nodePath: iconNode.path, property: 'texture', type: iconNode.type, props: iconNode.props, assetId, usages: [usage], textureVariant: true }],
            nodes: [{ path: iconNode.path, type: iconNode.type, props: iconNode.props }], states: ['normal', 'hover', 'pressed', 'disabled'] });
    }
    return { schemaVersion: 1, fingerprint: hash(JSON.stringify(fingerprints.sort())), assets: [...assets.values()], components,
        warnings: [...new Set(warnings)], stats: { scenes: resolved.size, assets: assets.size, components: components.length } };
}

async function originalImage(root, asset) {
    if (!asset) throw new ValidationError('找不到原版图片');
    if (asset.source === 'color') {
        const [r, g, b, alpha] = asset.color;
        return sharp({ create: { width: asset.width, height: asset.height, channels: 4, background: { r: Math.round(r * 255), g: Math.round(g * 255), b: Math.round(b * 255), alpha } } }).png().toBuffer();
    }
    if (asset.source === 'image') return sharp(local(root, asset.id)).png().toBuffer();
    const [x, y, width, height] = asset.region;
    const [left, top, extraWidth, extraHeight] = asset.margin;
    const right = extraWidth - left, bottom = extraHeight - top;
    if ([x, y, width, height, left, top, right, bottom].some(v => !Number.isInteger(v) || v < 0) || !width || !height)
        throw new ValidationError('切片区域或透明留白无效');
    return sharp(local(root, asset.atlas)).extract({ left: x, top: y, width, height })
        .extend({ left, top, right, bottom, background: '#00000000' }).png().toBuffer();
}
module.exports = { scan, sections, vector, local, originalImage, DEFINITIONS, hash };
