const express = require('express');
const router = express.Router();
const { run, get, all } = require('../database');

// 获取所有设置
router.get('/', async (req, res) => {
    try {
        const settings = await all('SELECT * FROM settings');
        const result = {};
        settings.forEach(s => {
            result[s.key] = s.value;
        });
        res.json({ success: true, data: result });
    } catch (error) {
        res.status(500).json({ success: false, error: error.message });
    }
});

// 更新设置
router.put('/:key', async (req, res) => {
    try {
        const { key } = req.params;
        const { value } = req.body;
        
        if (!value) {
            return res.status(400).json({ success: false, error: 'Value is required' });
        }

        await run(
            'INSERT OR REPLACE INTO settings (key, value, updated_at) VALUES (?, ?, CURRENT_TIMESTAMP)',
            [key, value]
        );

        res.json({ success: true, message: 'Setting updated' });
    } catch (error) {
        res.status(500).json({ success: false, error: error.message });
    }
});

// 批量更新设置
router.put('/', async (req, res) => {
    try {
        const settings = req.body;
        
        for (const [key, value] of Object.entries(settings)) {
            await run(
                'INSERT OR REPLACE INTO settings (key, value, updated_at) VALUES (?, ?, CURRENT_TIMESTAMP)',
                [key, value]
            );
        }

        res.json({ success: true, message: 'Settings updated' });
    } catch (error) {
        res.status(500).json({ success: false, error: error.message });
    }
});

module.exports = router;
