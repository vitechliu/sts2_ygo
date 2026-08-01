const express = require('express');
const pathModule = require('path');
const router = express.Router();
const { runConfig, allConfig } = require('../database');
const {
    ValidationError,
    parseDatabaseId,
    normalizePriority,
    requireString
} = require('../services/inputValidation');

const ALLOWED_TYPES = new Set(['card_image', 'portrait', 'other']);

function normalizeDirectoryInput(body) {
    const directoryPath = pathModule.resolve(requireString(body.path, 'Path'));
    const type = requireString(body.type, 'Type', { maxLength: 32 });
    if (!ALLOWED_TYPES.has(type)) {
        throw new ValidationError('Invalid directory type');
    }
    return {
        path: directoryPath,
        type,
        priority: normalizePriority(body.priority),
        description: requireString(body.description ?? '', 'Description', { allowEmpty: true, maxLength: 512 })
    };
}

// 获取所有外部目录
router.get('/', async (req, res) => {
    try {
        const dirs = await allConfig('SELECT * FROM external_dirs ORDER BY priority DESC, id ASC');
        res.json({ success: true, data: dirs });
    } catch (error) {
        res.status(500).json({ success: false, error: error.message });
    }
});

// 添加外部目录
router.post('/', async (req, res) => {
    try {
        const input = normalizeDirectoryInput(req.body);

        const result = await runConfig(
            'INSERT INTO external_dirs (path, type, priority, description) VALUES (?, ?, ?, ?)',
            [input.path, input.type, input.priority, input.description]
        );

        res.json({ success: true, data: { id: result.id } });
    } catch (error) {
        if (error instanceof ValidationError) {
            return res.status(400).json({ success: false, error: error.message });
        }
        res.status(500).json({ success: false, error: error.message });
    }
});

// 更新外部目录
router.put('/:id', async (req, res) => {
    try {
        const id = parseDatabaseId(req.params.id);
        const input = normalizeDirectoryInput(req.body);

        const result = await runConfig(
            'UPDATE external_dirs SET path = ?, type = ?, priority = ?, description = ? WHERE id = ?',
            [input.path, input.type, input.priority, input.description, id]
        );
        if (result.changes === 0) {
            return res.status(404).json({ success: false, error: 'Directory not found' });
        }

        res.json({ success: true, message: 'Directory updated' });
    } catch (error) {
        if (error instanceof ValidationError) {
            return res.status(400).json({ success: false, error: error.message });
        }
        res.status(500).json({ success: false, error: error.message });
    }
});

// 删除外部目录
router.delete('/:id', async (req, res) => {
    try {
        const id = parseDatabaseId(req.params.id);
        const result = await runConfig('DELETE FROM external_dirs WHERE id = ?', [id]);
        if (result.changes === 0) {
            return res.status(404).json({ success: false, error: 'Directory not found' });
        }
        res.json({ success: true, message: 'Directory deleted' });
    } catch (error) {
        if (error instanceof ValidationError) {
            return res.status(400).json({ success: false, error: error.message });
        }
        res.status(500).json({ success: false, error: error.message });
    }
});

module.exports = router;
