// 仅供人工浏览器验证，使用正式播放器和 API；不自动修改工程或映射。
const express = require('express');
const fs = require('fs');
const path = require('path');
const app = express();
app.use(express.json({ limit: '1mb' }));
app.use('/api/ui-audio', require('../../routes/uiAudio'));
app.use('/js', express.static(path.resolve(__dirname, '../../public/js')));
app.get('/', (_, res) => res.sendFile(path.join(__dirname, 'fmod-preview.html')));
app.post('/results', (req, res) => {
    const directory = path.resolve(__dirname, '../../.audio-cache');
    fs.mkdirSync(directory, { recursive: true });
    fs.writeFileSync(path.join(directory, 'wasm-browser-results.json'), JSON.stringify(req.body, null, 2));
    res.json({ saved: true });
});
const port = Number(process.env.PORT || 3018);
app.listen(port, '127.0.0.1', () => console.log(`WASM 浏览器验证：http://localhost:${port}`));
