const express = require('express');
const router = express.Router();
const { runCards, getCards, allCards, allConfig } = require('../database');
const ygoApiService = require('../services/ygoApiService');
const imageService = require('../services/imageService');
const fs = require('fs');
const path = require('path');

// 获取所有卡牌
router.get('/', async (req, res) => {
    try {
        const cards = await allCards('SELECT * FROM cards ORDER BY created_at DESC');
        res.json({ success: true, data: cards });
    } catch (error) {
        res.status(500).json({ success: false, error: error.message });
    }
});

// 通过ID查询卡牌（同步查询API、卡图、立绘）
router.get('/query/:cardId', async (req, res) => {
    try {
        const { cardId } = req.params;
        
        // 并行执行三个查询
        const [apiData, cardImagePath, portraitPath] = await Promise.all([
            ygoApiService.getCardById(cardId).catch(err => {
                console.log(`API query failed for ${cardId}:`, err.message);
                return null;
            }),
            imageService.findImageFile(cardId).catch(err => {
                console.log(`Image search failed for ${cardId}:`, err.message);
                return null;
            }),
            imageService.findPortraitFile(cardId).catch(err => {
                console.log(`Portrait search failed for ${cardId}:`, err.message);
                return null;
            })
        ]);

        const result = {
            cardId,
            apiData: null,
            cardImage: null,
            portrait: null
        };

        // 解析API数据
        if (apiData) {
            result.apiData = ygoApiService.parseCardData(apiData);
            
            // 检查是否已存在
            const existingCard = await getCards('SELECT * FROM cards WHERE card_id = ?', [cardId]);
            result.apiData.exists = !!existingCard;
        }

        // 卡图信息
        if (cardImagePath) {
            result.cardImage = {
                found: true,
                path: cardImagePath,
                hasLocal: await imageService.imageExists(cardId)
            };
        } else {
            result.cardImage = {
                found: false,
                path: null,
                hasLocal: await imageService.imageExists(cardId)
            };
        }

        // 立绘信息
        if (portraitPath) {
            result.portrait = {
                found: true,
                path: portraitPath,
                hasLocal: await imageService.portraitExists(cardId)
            };
        } else {
            result.portrait = {
                found: false,
                path: null,
                hasLocal: await imageService.portraitExists(cardId)
            };
        }

        res.json({ success: true, data: result });
    } catch (error) {
        res.status(500).json({ success: false, error: error.message });
    }
});

// 添加本地化条目
router.post('/localization', async (req, res) => {
    try {
        const { cardId, enName, cnName } = req.body;
        
        if (!enName) {
            return res.status(400).json({ success: false, error: 'English name is required for localization' });
        }

        const localePath = await addLocalizationEntry(cardId, enName, cnName);
        
        res.json({ 
            success: true, 
            data: { localePath },
            message: 'Localization entry added' 
        });
    } catch (error) {
        res.status(500).json({ success: false, error: error.message });
    }
});

// 新增卡牌（处理裁剪图片、立绘、插入数据库）
router.post('/', async (req, res) => {
    try {
        const { 
            cardId, name, cnName, enName, types, description, 
            atk, def, level, attribute, race, rawData,
            cropParams, cardImagePath, portraitPath 
        } = req.body;

        // 检查是否已存在
        const existing = await getCards('SELECT * FROM cards WHERE card_id = ?', [cardId]);
        if (existing) {
            return res.status(400).json({ success: false, error: 'Card already exists' });
        }

        let finalImagePath = null;
        let finalPortraitPath = null;

        // 处理卡图（如果有裁剪参数和卡图源文件）
        if (cardImagePath && fs.existsSync(cardImagePath)) {
            try {
                finalImagePath = await imageService.processCroppedImage(
                    cardImagePath, 
                    cardId, 
                    cropParams || null
                );
            } catch (e) {
                console.log('Image processing failed:', e.message);
            }
        }

        // 处理立绘（直接复制）
        if (portraitPath && fs.existsSync(portraitPath)) {
            try {
                finalPortraitPath = await imageService.copyPortrait(portraitPath, cardId);
            } catch (e) {
                console.log('Portrait copy failed:', e.message);
            }
        }

        // 插入数据库
        const result = await runCards(
            `INSERT INTO cards (card_id, name, cn_name, en_name, types, description, atk, def, level, attribute, race, raw_data) 
             VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
            [cardId, name, cnName, enName, types, description, atk, def, level, attribute, race, rawData]
        );

        res.json({ 
            success: true, 
            data: { 
                id: result.id, 
                cardId, 
                imagePath: finalImagePath,
                portraitPath: finalPortraitPath,
                message: 'Card added successfully' 
            } 
        });
    } catch (error) {
        res.status(500).json({ success: false, error: error.message });
    }
});

// 删除卡牌
router.delete('/:cardId', async (req, res) => {
    try {
        const { cardId } = req.params;
        
        // 先获取卡牌信息（用于本地化删除）
        const card = await getCards('SELECT en_name FROM cards WHERE card_id = ?', [cardId]);
        
        // 从本地化文件中移除
        await removeLocalizationEntry(cardId, card?.en_name);
        
        // 删除数据库记录
        await runCards('DELETE FROM cards WHERE card_id = ?', [cardId]);
        
        // 删除卡图文件
        const imagePath = imageService.getImagePath(cardId);
        if (fs.existsSync(imagePath)) {
            fs.unlinkSync(imagePath);
        }

        // 删除立绘文件
        const portraitPath = imageService.getPortraitPath(cardId);
        if (fs.existsSync(portraitPath)) {
            fs.unlinkSync(portraitPath);
        }

        res.json({ success: true, message: 'Card deleted' });
    } catch (error) {
        res.status(500).json({ success: false, error: error.message });
    }
});

// 生成 Godot 场景
router.post('/:cardId/scene', async (req, res) => {
    try {
        const { cardId } = req.params;
        const sceneDir = path.join(__dirname, '..', '..', 'VYgo', 'scenes', 'monsters');
        const samplePath = path.join(sceneDir, '0_sample.tscn');
        const targetPath = path.join(sceneDir, `${cardId}.tscn`);

        if (!fs.existsSync(samplePath)) {
            return res.status(404).json({ success: false, error: 'Scene template not found' });
        }

        if (!fs.existsSync(sceneDir)) {
            fs.mkdirSync(sceneDir, { recursive: true });
        }

        let content = fs.readFileSync(samplePath, 'utf8');
        // 移除场景 uid，让 Godot 重新生成
        content = content.replace(/\[gd_scene load_steps=(\d+) format=(\d+) uid="[^"]+"\]/, '[gd_scene load_steps=$1 format=$2]');
        // 替换图片路径并移除 Texture2D ext_resource 的 uid
        content = content.replace(
            /\[ext_resource type="Texture2D" uid="[^"]+" path="res:\/\/VYgo\/images\/monster\/[^"]+" id="([^"]+)"\]/,
            `[ext_resource type="Texture2D" path="res://VYgo/images/monster/${cardId}.png" id="$1"]`
        );

        fs.writeFileSync(targetPath, content, 'utf8');

        res.json({ success: true, data: { scenePath: targetPath } });
    } catch (error) {
        res.status(500).json({ success: false, error: error.message });
    }
});

// 导出所有卡牌数据为 JSON
router.post('/export', async (req, res) => {
    try {
        const cards = await allCards('SELECT * FROM cards ORDER BY created_at DESC');
        const vygoDir = path.join(__dirname, '..', '..', 'VYgo');
        const exportPath = path.join(vygoDir, 'db.json');

        if (!fs.existsSync(vygoDir)) {
            fs.mkdirSync(vygoDir, { recursive: true });
        }

        const filteredCards = cards.map(card => {
            const { raw_data, created_at, updated_at, ...rest } = card;
            return rest;
        });

        fs.writeFileSync(exportPath, JSON.stringify(filteredCards, null, 4), 'utf8');

        res.json({ success: true, data: { exportPath, count: filteredCards.length } });
    } catch (error) {
        res.status(500).json({ success: false, error: error.message });
    }
});

// 手动触发本地化生成（全量重新生成）
router.post('/localization/generate', async (req, res) => {
    try {
        await generateLocalization();
        res.json({ success: true, message: 'Localization generated' });
    } catch (error) {
        res.status(500).json({ success: false, error: error.message });
    }
});

// 图片预览（用于加载外部目录的图片）
router.get('/image-preview', async (req, res) => {
    try {
        const filePath = req.query.path;
        if (!filePath) {
            return res.status(400).json({ success: false, error: 'Path is required' });
        }
        
        // 安全检查：确保路径存在且是文件
        if (!fs.existsSync(filePath) || !fs.statSync(filePath).isFile()) {
            return res.status(404).json({ success: false, error: 'Image not found' });
        }
        
        res.sendFile(path.resolve(filePath));
    } catch (error) {
        res.status(500).json({ success: false, error: error.message });
    }
});

// 获取单张卡牌详情
router.get('/:cardId', async (req, res) => {
    try {
        const { cardId } = req.params;
        const card = await getCards('SELECT * FROM cards WHERE card_id = ?', [cardId]);
        
        if (!card) {
            return res.status(404).json({ success: false, error: 'Card not found' });
        }

        res.json({ success: true, data: card });
    } catch (error) {
        res.status(500).json({ success: false, error: error.message });
    }
});

// 辅助函数：将驼峰命名转换为大写下划线命名
function toUpperSnakeCase(str) {
    if (!str) return '';
    return str
        .replace(/([A-Z])/g, '_$1')
        .toUpperCase()
        .replace(/^_/, '');
}

// 辅助函数：添加单条本地化条目
async function addLocalizationEntry(cardId, enName, cnName) {
    const settings = await allConfig('SELECT * FROM settings');
    const config = {};
    settings.forEach(s => config[s.key] = s.value);
    
    const prefix = config.locale_prefix || 'V_YGO_CARD_';
    const localeDir = path.join(__dirname, '..', '..', 'VYgo', 'localization', 'zhs');
    
    if (!fs.existsSync(localeDir)) {
        fs.mkdirSync(localeDir, { recursive: true });
    }

    // 转换为大写下划线格式
    const upperSnakeName = toUpperSnakeCase(enName);
    const key = `${prefix}${upperSnakeName}`;

    // 更新 cards.json
    const cardsLocalePath = path.join(localeDir, 'cards.json');
    let cardsLocalization = {};
    
    if (fs.existsSync(cardsLocalePath)) {
        try {
            const content = fs.readFileSync(cardsLocalePath, 'utf8');
            cardsLocalization = JSON.parse(content);
        } catch (e) {
            console.log('Failed to parse existing cards localization, creating new');
        }
    }

    cardsLocalization[`${key}.title`] = cnName || enName;
    // 描述留空或从数据库获取
    const card = await getCards('SELECT description FROM cards WHERE card_id = ?', [cardId]);
    cardsLocalization[`${key}.description`] = card?.description || '';

    fs.writeFileSync(cardsLocalePath, JSON.stringify(cardsLocalization, null, 4), 'utf8');

    // 更新 monsters.json
    const monstersLocalePath = path.join(localeDir, 'monsters.json');
    let monstersLocalization = {};
    
    if (fs.existsSync(monstersLocalePath)) {
        try {
            const content = fs.readFileSync(monstersLocalePath, 'utf8');
            monstersLocalization = JSON.parse(content);
        } catch (e) {
            console.log('Failed to parse existing monsters localization, creating new');
        }
    }

    monstersLocalization[`${upperSnakeName}_MINION.name`] = cnName || enName;
    fs.writeFileSync(monstersLocalePath, JSON.stringify(monstersLocalization, null, 4), 'utf8');
    
    console.log(`Localization entry added: ${key}`);
    return cardsLocalePath;
}

// 辅助函数：移除单条本地化条目
async function removeLocalizationEntry(cardId, enName) {
    const settings = await allConfig('SELECT * FROM settings');
    const config = {};
    settings.forEach(s => config[s.key] = s.value);
    
    const prefix = config.locale_prefix || 'V_YGO_CARD_';
    const localeDir = path.join(__dirname, '..', '..', 'VYgo', 'localization', 'zhs');
    
    // 从 cards.json 中移除
    const cardsLocalePath = path.join(localeDir, 'cards.json');
    if (fs.existsSync(cardsLocalePath)) {
        try {
            const content = fs.readFileSync(cardsLocalePath, 'utf8');
            const localization = JSON.parse(content);
            
            if (enName) {
                const upperSnakeName = toUpperSnakeCase(enName);
                const key = `${prefix}${upperSnakeName}`;
                delete localization[`${key}.title`];
                delete localization[`${key}.description`];
            } else {
                // 如果没有 enName，回退到旧逻辑
                Object.keys(localization).forEach(key => {
                    if (key.includes(cardId)) {
                        delete localization[key];
                    }
                });
            }

            fs.writeFileSync(cardsLocalePath, JSON.stringify(localization, null, 4), 'utf8');
        } catch (e) {
            console.error('Failed to remove cards localization entry:', e);
        }
    }
    
    // 从 monsters.json 中移除
    const monstersLocalePath = path.join(localeDir, 'monsters.json');
    if (fs.existsSync(monstersLocalePath)) {
        try {
            const content = fs.readFileSync(monstersLocalePath, 'utf8');
            const localization = JSON.parse(content);
            
            if (enName) {
                const upperSnakeName = toUpperSnakeCase(enName);
                delete localization[`${upperSnakeName}_MINION.name`];
            } else {
                // 如果没有 enName，回退到旧逻辑
                Object.keys(localization).forEach(key => {
                    if (key.includes(cardId)) {
                        delete localization[key];
                    }
                });
            }

            fs.writeFileSync(monstersLocalePath, JSON.stringify(localization, null, 4), 'utf8');
        } catch (e) {
            console.error('Failed to remove monsters localization entry:', e);
        }
    }
    
    console.log(`Localization entries removed for card ${cardId}`);
}

// 增量生成本地化 JSON：只插入不存在的键，不更新已有键
async function generateLocalization() {
    try {
        const settings = await allConfig('SELECT * FROM settings');
        const config = {};
        settings.forEach(s => config[s.key] = s.value);

        const prefix = config.locale_prefix || 'V_YGO_CARD_';
        const cards = await allCards('SELECT * FROM cards ORDER BY card_id');

        const localeDir = path.join(__dirname, '..', '..', 'VYgo', 'localization', 'zhs');
        if (!fs.existsSync(localeDir)) {
            fs.mkdirSync(localeDir, { recursive: true });
        }

        const cardsLocalePath = path.join(localeDir, 'cards.json');
        const cardsLocalization = loadJson(cardsLocalePath);

        const monstersLocalePath = path.join(localeDir, 'monsters.json');
        const monstersLocalization = loadJson(monstersLocalePath);

        let insertedCount = 0;

        cards.forEach(card => {
            const upperSnakeName = toUpperSnakeCase(card.en_name);
            const key = `${prefix}${upperSnakeName}`;
            const titleKey = `${key}.title`;
            const descKey = `${key}.description`;
            const monsterKey = `${upperSnakeName}_MINION.name`;

            // 只在键不存在时插入
            if (!cardsLocalization.hasOwnProperty(titleKey)) {
                cardsLocalization[titleKey] = card.cn_name;
                insertedCount++;
            }
            if (!cardsLocalization.hasOwnProperty(descKey)) {
                cardsLocalization[descKey] = '';
            }

            if (!monstersLocalization.hasOwnProperty(monsterKey)) {
                monstersLocalization[monsterKey] = card.cn_name;
            }
        });

        fs.writeFileSync(cardsLocalePath, JSON.stringify(cardsLocalization, null, 4), 'utf8');
        fs.writeFileSync(monstersLocalePath, JSON.stringify(monstersLocalization, null, 4), 'utf8');

        console.log(`Localization files updated incrementally: ${insertedCount} new entries inserted.`);
    } catch (error) {
        console.error('Failed to generate localization:', error);
    }
}

function loadJson(filePath) {
    if (!fs.existsSync(filePath)) return {};
    try {
        const content = fs.readFileSync(filePath, 'utf8');
        return JSON.parse(content);
    } catch (e) {
        console.log('Failed to parse JSON, returning empty object:', filePath);
        return {};
    }
}

module.exports = router;
