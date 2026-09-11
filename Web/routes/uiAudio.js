const express = require('express');
const service = require('../services/uiAudioService');
const { runtimeFile, runtimeInfo } = require('../services/fmodRuntime');
const router = express.Router();
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
router.get('/runtime', route(runtimeInfo));
router.put('/settings', route(service.saveSettings));
router.post('/profiles', route(service.createProfile));
router.put('/profiles/:id', async (req, res) => {
    try { res.json(await service.createProfile(req.body, req.params.id)); }
    catch (error) { res.status(400).json({ error: error.message }); }
});
router.put('/mapping', route(service.saveMapping));
router.post('/playback', route(service.playback));
router.get('/runtime/:name', (req, res) => {
    try {
        res.setHeader('Cache-Control', 'no-cache');
        res.type(req.params.name.endsWith('.wasm') ? 'application/wasm' : 'application/javascript').sendFile(runtimeFile(req.params.name));
    } catch (error) { res.status(503).json({ error: error.message }); }
});
router.get('/banks/:id', (req, res) => {
    try {
        const file = service.bankFile(req.params.id);
        res.setHeader('Cache-Control', 'private, max-age=31536000, immutable');
        res.type('application/octet-stream').sendFile(file);
    } catch (error) { res.status(404).json({ error: error.message }); }
});
module.exports = router;
