const express = require('express');
const router = express.Router();
const { runConfig, allConfig } = require('../database');
const { ValidationError, requireString } = require('../services/inputValidation');

function normalizeSettingEntry(key, value) {
    return {
        key: requireString(key, 'Setting key', { maxLength: 128 }),
        value: requireString(value, 'Value', { allowEmpty: true, maxLength: 4096 })
    };
}

// 获取所有设置
router.get('/', async (req, res) => {
    try {
        const settings = await allConfig('SELECT * FROM settings');
        const result = Object.create(null);
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
        const setting = normalizeSettingEntry(req.params.key, req.body.value);

        await runConfig(
            'INSERT OR REPLACE INTO settings (key, value, updated_at) VALUES (?, ?, CURRENT_TIMESTAMP)',
            [setting.key, setting.value]
        );

        res.json({ success: true, message: 'Setting updated' });
    } catch (error) {
        if (error instanceof ValidationError) {
            return res.status(400).json({ success: false, error: error.message });
        }
        res.status(500).json({ success: false, error: error.message });
    }
});

// 批量更新设置
router.put('/', async (req, res) => {
    try {
        const settings = req.body;
        if (settings === null || typeof settings !== 'object' || Array.isArray(settings)) {
            throw new ValidationError('Settings must be an object');
        }

        const normalizedSettings = Object.entries(settings)
            .map(([key, value]) => normalizeSettingEntry(key, value));
        for (const setting of normalizedSettings) {
            await runConfig(
                'INSERT OR REPLACE INTO settings (key, value, updated_at) VALUES (?, ?, CURRENT_TIMESTAMP)',
                [setting.key, setting.value]
            );
        }

        res.json({ success: true, message: 'Settings updated' });
    } catch (error) {
        if (error instanceof ValidationError) {
            return res.status(400).json({ success: false, error: error.message });
        }
        res.status(500).json({ success: false, error: error.message });
    }
});

module.exports = router;
