const express = require('express');
const { UiSkinService, sourceFile } = require('../services/uiSkin/service');
const { ValidationError } = require('../services/inputValidation');

function createRouter(service = new UiSkinService()) {
    const router = express.Router();
    // 此接口仅操作配置中的本地工程，不允许跨站页面触发文件写入。
    router.use((req, res, next) => {
        if (!['GET', 'HEAD'].includes(req.method) && req.headers.origin) {
            try { if (new URL(req.headers.origin).host !== req.headers.host) return res.status(403).json({ error: '不允许跨站请求' }); }
            catch { return res.status(403).json({ error: '无效来源' }); }
        }
        next();
    });
    const action = handler => async (req, res) => {
        try { await handler(req, res); }
        catch (error) { res.status(error instanceof ValidationError ? 400 : 500).json({ error: error.message }); }
    };
    router.get('/catalog', action(async (_, res) => res.json(service.getCatalog())));
    router.post('/scan', action(async (req, res) => res.json(await service.scan(req.body.root))));
    router.get('/config', action(async (_, res) => res.json(service.config())));
    router.put('/config', action(async (req, res) => res.json(await service.save(req.body))));
    router.get('/sources', action(async (_, res) => res.json(service.read('sources.json', []))));
    router.post('/sources', express.raw({ type: 'image/png', limit: '20mb' }), action(async (req, res) => res.json(await service.importImage(req.body, req.query.name))));
    router.get('/sources/:id', action(async (req, res) => res.sendFile(sourceFile(service.base, req.params.id))));
    router.get('/original', action(async (req, res) => res.type('png').send(await service.original(req.query.id))));
    router.post('/preview', action(async (req, res) => res.type('png').send((await service.preview(req.body.rule, req.body.state || 'normal', req.body.size)).png)));
    router.post('/export', action(async (_, res) => res.json(await service.export())));
    router.post('/capture', action(async (req, res) => res.json(await service.capture(req.body))));
    return router;
}
module.exports = createRouter();
module.exports.createRouter = createRouter;
