const express = require('express');
const router = express.Router();
const { run, get, all } = require('../database');
const ygoApiService = require('../services/ygoApiService');
const imageService = require('../services/imageService');
const fs = require('fs');
const path = require('path');

// 获取所有卡牌
router.get('/', async (req, res) => {
    try {
        const cards = await all('SELECT * FROM cards ORDER BY created_at DESC');
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
            const existingCard = await get('SELECT * FROM cards WHERE card_id = ?', [cardId]);
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
        const existing = await get('SELECT * FROM cards WHERE card_id = ?', [cardId]);
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
        const result = await run(
            `INSERT INTO cards (card_id, name, cn_name, types, description, atk, def, level, attribute, race, raw_data, image_path) 
             VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
            [cardId, name, cnName, types, description, atk, def, level, attribute, race, rawData, finalImagePath]
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
        
        // 删除数据库记录
        await run('DELETE FROM cards WHERE card_id = ?', [cardId]);
        
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

        // 从本地化文件中移除
        await removeLocalizationEntry(cardId);

        res.json({ success: true, message: 'Card deleted' });
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
        const card = await get('SELECT * FROM cards WHERE card_id = ?', [cardId]);
        
        if (!card) {
            return res.status(404).json({ success: false, error: 'Card not found' });
        }

        res.json({ success: true, data: card });
    } catch (error) {
        res.status(500).json({ success: false, error: error.message });
    }
});

// 辅助函数：添加单条本地化条目
async function addLocalizationEntry(cardId, enName, cnName) {
    const settings = await all('SELECT * FROM settings');
    const config = {};
    settings.forEach(s => config[s.key] = s.value);
    
    const prefix = config.locale_prefix || 'REGENT_PLUS_CARD_';
    const localeDir = path.join(__dirname, '..', '..', 'VYgo', 'localization', 'zhs');
    
    if (!fs.existsSync(localeDir)) {
        fs.mkdirSync(localeDir, { recursive: true });
    }

    const localePath = path.join(localeDir, 'cards.json');
    let localization = {};
    
    // 读取现有文件
    if (fs.existsSync(localePath)) {
        try {
            const content = fs.readFileSync(localePath, 'utf8');
            localization = JSON.parse(content);
        } catch (e) {
            console.log('Failed to parse existing localization, creating new');
        }
    }

    // 添加/更新条目
    const key = `${prefix}${enName}`;
    localization[`${key}.title`] = cnName || enName;
    // 描述留空或从数据库获取
    const card = await get('SELECT description FROM cards WHERE card_id = ?', [cardId]);
    localization[`${key}.description`] = card?.description || '';

    // 写入文件
    fs.writeFileSync(localePath, JSON.stringify(localization, null, 4), 'utf8');
    
    console.log(`Localization entry added: ${key}`);
    return localePath;
}

// 辅助函数：移除单条本地化条目
async function removeLocalizationEntry(cardId) {
    const settings = await all('SELECT * FROM settings');
    const config = {};
    settings.forEach(s => config[s.key] = s.value);
    
    const prefix = config.locale_prefix || 'REGENT_PLUS_CARD_';
    const localeDir = path.join(__dirname, '..', '..', 'VYgo', 'localization', 'zhs');
    const localePath = path.join(localeDir, 'cards.json');
    
    if (!fs.existsSync(localePath)) return;

    try {
        const content = fs.readFileSync(localePath, 'utf8');
        const localization = JSON.parse(content);
        
        // 查找并删除该cardId相关的条目（需要知道enName，这里简单处理：删除所有匹配cardId的键）
        // 实际上我们需要通过数据库查询获取enName
        const card = await get('SELECT card_id FROM cards WHERE card_id = ?', [cardId]);
        
        // 由于我们不知道enName，这里简单删除所有以该cardId结尾的键
        // 更准确的方案：从cards表查询en_name字段，但表结构没有en_name字段
        // 临时方案：读取所有键，查找包含cardId的键
        Object.keys(localization).forEach(key => {
            if (key.includes(cardId)) {
                delete localization[key];
            }
        });

        fs.writeFileSync(localePath, JSON.stringify(localization, null, 4), 'utf8');
        console.log(`Localization entries removed for card ${cardId}`);
    } catch (e) {
        console.error('Failed to remove localization entry:', e);
    }
}

// 全量生成本地化 JSON
async function generateLocalization() {
    try {
        const settings = await all('SELECT * FROM settings');
        const config = {};
        settings.forEach(s => config[s.key] = s.value);
        
        const prefix = config.locale_prefix || 'REGENT_PLUS_CARD_';
        const cards = await all('SELECT * FROM cards ORDER BY card_id');
        
        const localization = {};
        cards.forEach(card => {
            const key = `${prefix}${card.card_id}`;
            localization[`${key}.title`] = card.cn_name || card.name;
            localization[`${key}.description`] = card.description || '';
        });

        const localeDir = path.join(__dirname, '..', '..', 'VYgo', 'localization', 'zhs');
        if (!fs.existsSync(localeDir)) {
            fs.mkdirSync(localeDir, { recursive: true });
        }

        const localePath = path.join(localeDir, 'cards.json');
        fs.writeFileSync(localePath, JSON.stringify(localization, null, 4), 'utf8');
        
        console.log(`Localization file generated: ${localePath}`);
    } catch (error) {
        console.error('Failed to generate localization:', error);
    }
}

module.exports = router;
