const express = require('express');
const path = require('path');
const { exec } = require('child_process');

const app = express();
const PORT = process.env.PORT || 3000;

app.use(express.json({ limit: '50mb' }));
app.use(express.static(path.join(__dirname, 'public')));

// 静态资源代理：让前端能访问项目根目录的 VYgo 文件夹
app.use('/VYgo', express.static(path.join(__dirname, '..', 'VYgo')));

// 路由
app.use('/api/settings', require('./routes/settings'));
app.use('/api/external-dirs', require('./routes/externalDirs'));
app.use('/api/cards', require('./routes/cards'));

// 根路径返回前端页面
app.get('/', (req, res) => {
    res.sendFile(path.join(__dirname, 'public', 'index.html'));
});

// 错误处理
app.use((err, req, res, next) => {
    console.error(err.stack);
    res.status(500).json({ success: false, error: 'Internal server error' });
});

function openBrowser(url) {
    const command = process.platform === 'win32' ? `start "" "${url}"` :
                    process.platform === 'darwin' ? `open "${url}"` :
                    `xdg-open "${url}"`;
    exec(command, (err) => {
        if (err) {
            console.log(`Could not open browser automatically: ${err.message}`);
        }
    });
}

app.listen(PORT, () => {
    const url = `http://localhost:${PORT}`;
    console.log(`VYgo Card Manager running at ${url}`);
    console.log(`Press Ctrl+C to stop.`);
    openBrowser(url);
});

module.exports = app;
