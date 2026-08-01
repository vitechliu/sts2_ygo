const express = require('express');
const path = require('path');
const { execFile } = require('child_process');

const app = express();
const PORT = parsePort(process.env.PORT || '3000');
const HOST = process.env.HOST || '127.0.0.1';
const shouldOpenBrowser = !['0', 'false', 'no'].includes(
    String(process.env.OPEN_BROWSER || 'true').toLowerCase()
);

app.use(express.json({ limit: '2mb' }));
app.use((req, res, next) => {
    res.setHeader('X-Content-Type-Options', 'nosniff');
    res.setHeader('Referrer-Policy', 'no-referrer');
    next();
});
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
    const command = process.platform === 'win32' ? 'rundll32.exe' :
                    process.platform === 'darwin' ? 'open' : 'xdg-open';
    const args = process.platform === 'win32' ? ['url.dll,FileProtocolHandler', url] : [url];
    execFile(command, args, { windowsHide: true }, (err) => {
        if (err) {
            console.log(`Could not open browser automatically: ${err.message}`);
        }
    });
}

function parsePort(value) {
    const port = Number(value);
    if (!Number.isInteger(port) || port < 1 || port > 65535) {
        throw new Error('PORT must be an integer between 1 and 65535');
    }
    return port;
}

function startServer() {
    return app.listen(PORT, HOST, () => {
        const displayHost = HOST === '127.0.0.1' ? 'localhost' : HOST;
        const url = `http://${displayHost}:${PORT}`;
        console.log(`VYgo Card Manager running at ${url}`);
        console.log('Press Ctrl+C to stop.');
        if (shouldOpenBrowser) {
            openBrowser(url);
        } else {
            console.log('Browser auto-open disabled by OPEN_BROWSER.');
        }
    });
}

if (require.main === module) {
    startServer();
}

module.exports = { app, parsePort, startServer };
