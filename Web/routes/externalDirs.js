const express = require('express');
const router = express.Router();
const { run, get, all } = require('../database');

// 获取所有外部目录
router.get('/', async (req, res) => {
    try {
        const dirs = await all('SELECT * FROM external_dirs ORDER BY priority DESC, id ASC');
        res.json({ success: true, data: dirs });
    } catch (error) {
        res.status(500).json({ success: false, error: error.message });
    }
});

// 添加外部目录
router.post('/', async (req, res) => {
    try {
        const { path, type, priority, description } = req.body;
        
        if (!path || !type) {
            return res.status(400).json({ success: false, error: 'Path and type are required' });
        }

        const result = await run(
            'INSERT INTO external_dirs (path, type, priority, description) VALUES (?, ?, ?, ?)',
            [path, type, priority || 0, description || '']
        );

        res.json({ success: true, data: { id: result.id } });
    } catch (error) {
        res.status(500).json({ success: false, error: error.message });
    }
});

// 更新外部目录
router.put('/:id', async (req, res) => {
    try {
        const { id } = req.params;
        const { path, type, priority, description } = req.body;

        await run(
            'UPDATE external_dirs SET path = ?, type = ?, priority = ?, description = ? WHERE id = ?',
            [path, type, priority, description, id]
        );

        res.json({ success: true, message: 'Directory updated' });
    } catch (error) {
        res.status(500).json({ success: false, error: error.message });
    }
});

// 删除外部目录
router.delete('/:id', async (req, res) => {
    try {
        const { id } = req.params;
        await run('DELETE FROM external_dirs WHERE id = ?', [id]);
        res.json({ success: true, message: 'Directory deleted' });
    } catch (error) {
        res.status(500).json({ success: false, error: error.message });
    }
});

module.exports = router;
