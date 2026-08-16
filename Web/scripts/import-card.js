#!/usr/bin/env node
// 一键导入卡牌脚本：卡图（默认居中裁剪）、本地化、卡牌数据、立绘/场景/怪兽脚本（仅怪兽）、卡牌脚本
// 用法: node scripts/import-card.js <卡牌ID|卡牌名> --pool <卡池> [--en-name <英文名>]

const { app } = require('../server');

function parseArgs(argv) {
    const args = { query: null, pool: null, enName: null };
    const positional = [];

    for (let i = 0; i < argv.length; i++) {
        const arg = argv[i];
        if (arg === '--pool') {
            args.pool = argv[++i] ?? null;
        } else if (arg === '--en-name') {
            args.enName = argv[++i] ?? null;
        } else if (arg === '--help' || arg === '-h') {
            args.help = true;
        } else if (arg.startsWith('--')) {
            throw new Error(`未知参数: ${arg}`);
        } else {
            positional.push(arg);
        }
    }

    args.query = positional.join(' ').trim() || null;
    return args;
}

function printUsage() {
    console.log(`用法: node scripts/import-card.js <卡牌ID|卡牌名> --pool <卡池> [--en-name <英文名>]

参数:
  <卡牌ID|卡牌名>   游戏王卡牌 ID（如 70095154）或卡牌名（如 电子龙）
  --pool           卡池，支持 poolName（如 CommonCardPool）或文件夹简写（如 Common），不区分大小写
  --en-name        API 缺少英文名时手动指定（清洗后需为合法标识符）

示例:
  node scripts/import-card.js 76145933 --pool CommonCardPool
  node scripts/import-card.js 卫星闪灵·蓝色喷流灵 --pool Common`);
}

class ImportError extends Error {}

function createClient(baseUrl) {
    async function request(method, endpoint, body) {
        const response = await fetch(`${baseUrl}${endpoint}`, {
            method,
            headers: body ? { 'Content-Type': 'application/json' } : undefined,
            body: body ? JSON.stringify(body) : undefined
        });

        let result;
        try {
            result = await response.json();
        } catch {
            throw new ImportError(`${method} ${endpoint} 返回了非 JSON 响应 (HTTP ${response.status})`);
        }

        if (!result.success) {
            throw new ImportError(`${method} ${endpoint} 失败: ${result.error || `HTTP ${response.status}`}`);
        }
        return result;
    }

    return {
        get: (endpoint) => request('GET', endpoint),
        post: (endpoint, body) => request('POST', endpoint, body)
    };
}

async function resolveCardId(client, query) {
    if (/^\d+$/.test(query)) {
        return query;
    }

    const searchResult = await client.get(`/api/cards/search?query=${encodeURIComponent(query)}`);
    const cards = Array.isArray(searchResult.data) ? searchResult.data : [];
    if (cards.length === 0) {
        throw new ImportError(`没有找到与“${query}”匹配的卡牌`);
    }

    const exactMatches = cards.filter(card => card.isExactMatch);
    if (exactMatches.length === 1) {
        return String(exactMatches[0].cardId);
    }

    const candidates = cards
        .slice(0, 10)
        .map(card => `  - ${card.cardId} ${card.cnName || card.enName || ''}`)
        .join('\n');
    throw new ImportError(`“${query}”匹配到多个候选，请改用卡牌 ID:\n${candidates}`);
}

async function resolvePool(client, poolInput) {
    const optionsResult = await client.get('/api/cards/card-script-options');
    const options = optionsResult.data || [];
    const normalized = String(poolInput).toLowerCase();

    const matched = options.find(option => (
        option.poolName.toLowerCase() === normalized
        || option.folderName.toLowerCase() === normalized
    ));

    if (!matched) {
        const available = options.map(option => `  - ${option.label}（pool: ${option.poolName}，简写: ${option.folderName}）`).join('\n');
        throw new ImportError(`无效卡池: ${poolInput}\n可用卡池:\n${available}`);
    }

    return matched;
}

async function main() {
    const args = parseArgs(process.argv.slice(2));
    if (args.help) {
        printUsage();
        return;
    }
    if (!args.query) {
        printUsage();
        throw new ImportError('缺少卡牌 ID 或卡牌名');
    }
    if (!args.pool) {
        printUsage();
        throw new ImportError('缺少 --pool 参数');
    }

    const server = await new Promise((resolve, reject) => {
        const instance = app.listen(0, '127.0.0.1', () => resolve(instance));
        instance.on('error', reject);
    });

    const generated = [];
    const skipped = [];

    try {
        const baseUrl = `http://127.0.0.1:${server.address().port}`;
        const client = createClient(baseUrl);

        // 0. 预先解析卡池，避免卡池名错误导致半成品导入
        const pool = await resolvePool(client, args.pool);

        // 1. 解析卡牌并查询
        const cardId = await resolveCardId(client, args.query);
        console.log(`[1/7] 查询卡牌 ${cardId} ...`);
        const queryResult = await client.get(`/api/cards/query/${encodeURIComponent(cardId)}`);
        const { apiData, cardImage, portrait } = queryResult.data;

        if (!apiData) {
            throw new ImportError(`API 查询失败，无法获取卡牌 ${cardId} 的数据`);
        }
        if (apiData.exists) {
            throw new ImportError(`卡牌 ${cardId} 已存在，无需重复导入`);
        }

        const enName = apiData.enName || (args.enName || '').replace(/[^a-zA-Z0-9]/g, '');
        if (!/^[A-Za-z][A-Za-z0-9]*$/.test(enName)) {
            throw new ImportError('API 缺少英文名，请使用 --en-name 指定（清洗后需以字母开头）');
        }

        const isMonster = String(apiData.types || '').includes('怪兽');
        console.log(`      ${apiData.cnName || apiData.name} · ${enName} · ${apiData.types}`);

        // 2. 创建卡牌（cropParams 为 null，使用默认居中 cover 裁剪）
        console.log('[2/7] 创建卡牌（默认居中裁剪卡图）...');
        if (!cardImage?.found) {
            console.log('      警告: 未找到外部卡图，跳过卡图生成');
        }
        const createResult = await client.post('/api/cards', {
            cardId,
            name: apiData.name,
            cnName: apiData.cnName,
            enName,
            types: apiData.types,
            description: apiData.description,
            atk: apiData.atk,
            def: apiData.def,
            level: apiData.level,
            attribute: apiData.attribute,
            race: apiData.race,
            rawData: apiData.rawData,
            cropParams: null,
            cardImagePath: cardImage?.path || null,
            portraitPath: portrait?.path || null
        });
        if (createResult.data.imagePath) {
            generated.push(`卡图: ${createResult.data.imagePath}`);
        }
        if (createResult.data.portraitPath) {
            generated.push(`立绘: ${createResult.data.portraitPath}`);
        }

        // 3. 本地化
        console.log('[3/7] 生成本地化...');
        const localeResult = await client.post(`/api/cards/${cardId}/localization`);
        generated.push(`本地化: ${localeResult.data.localePath}`);

        // 4. 卡牌数据
        console.log('[4/7] 生成卡牌数据 (db.json)...');
        const dataResult = await client.post(`/api/cards/${cardId}/data`);
        generated.push(`卡牌数据: ${dataResult.data.exportPath}`);
        for (const warning of dataResult.data.warnings || []) {
            console.log(`      字段警告 (${warning.cardId}): ${warning.reason}`);
        }

        // 5-6. 怪兽专属：立绘、场景、怪兽脚本
        if (isMonster) {
            if (!createResult.data.portraitPath) {
                console.log('[5/7] 生成卡牌立绘...');
                try {
                    const portraitResult = await client.post(`/api/cards/${cardId}/portrait`);
                    generated.push(`立绘: ${portraitResult.data.portraitPath}`);
                } catch (error) {
                    skipped.push(`立绘（${error.message}）`);
                }
            } else {
                console.log('[5/7] 立绘已随创建生成，跳过');
            }

            console.log('[6/7] 生成怪兽场景与怪兽脚本...');
            const sceneResult = await client.post(`/api/cards/${cardId}/scene`);
            generated.push(`场景: ${sceneResult.data.scenePath}`);
            const monsterScriptResult = await client.post(`/api/cards/${cardId}/monster-script`);
            generated.push(`怪兽脚本: ${monsterScriptResult.data.scriptPath}`);
        } else {
            console.log('[5/7] 非怪兽卡，跳过立绘');
            console.log('[6/7] 非怪兽卡，跳过场景与怪兽脚本');
        }

        // 7. 卡牌脚本
        console.log('[7/7] 生成卡牌脚本...');
        const cardScriptResult = await client.post(`/api/cards/${cardId}/card-script`, {
            poolName: pool.poolName,
            folderName: pool.folderName
        });
        generated.push(`卡牌脚本: ${cardScriptResult.data.scriptPath}`);

        console.log('\n导入完成，生成物:');
        for (const item of generated) {
            console.log(`  ✓ ${item}`);
        }
        for (const item of skipped) {
            console.log(`  - 跳过 ${item}`);
        }
    } finally {
        await new Promise(resolve => server.close(resolve));
    }
}

main().catch(error => {
    if (error instanceof ImportError) {
        console.error(`\n导入失败: ${error.message}`);
    } else {
        console.error('\n导入失败:', error);
    }
    process.exitCode = 1;
});
