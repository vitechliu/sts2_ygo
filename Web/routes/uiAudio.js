const express = require('express');
const fs = require('fs');
const service = require('../services/uiAudioService');
const router = express.Router();

// 本机管理操作只接受同源请求，防止其他网页静默修改本地资源。
router.use((req, res, next) => {
    const origin = req.get('origin');
    if (origin && origin !== `${req.protocol}://${req.get('host')}`) return res.status(403).json({ error: '仅接受后台页面的同源请求。' });
    next();
});
function route(action) {
    return async (req, res) => {
        try { res.json(await action(req.body)); }
        catch (error) { res.status(400).json({ error: error.message }); }
    };
}
router.get('/', route(() => service.state()));
router.put('/settings', route(service.saveSettings));
router.post('/profiles', route(service.createProfile));
router.put('/profiles/:id', async (req, res) => {
    try { res.json(await service.createProfile(req.body, req.params.id)); }
    catch (error) { res.status(400).json({ error: error.message }); }
});
router.put('/mapping', route(service.saveMapping));
router.post('/preview', async (req, res) => {
    const controller = new AbortController();
    const abort = () => controller.abort();
    res.on('close', abort);
    try {
        const result = await service.preview(req.body, controller.signal);
        res.setHeader('X-Audio-Duration', result.duration);
        res.setHeader('X-Audio-Peak', result.peak);
        res.setHeader('Cache-Control', 'no-store');
        res.type('wav').sendFile(result.output, () => fs.rmSync(result.output, { force: true }));
    } catch (error) {
        if (!res.destroyed) res.status(400).json({ error: error.message });
    }
});
module.exports = router;
