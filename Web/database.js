const sqlite3 = require('sqlite3').verbose();
const path = require('path');

const WEB_DIR = __dirname;

// 个人配置数据库（不通过git追踪）
const CONFIG_DB_PATH = path.join(WEB_DIR, 'config.db');
// 卡牌数据库（通过git追踪，团队共享）
const CARDS_DB_PATH = path.join(WEB_DIR, 'cards.db');

// ===================== 配置数据库 =====================

const configDb = new sqlite3.Database(CONFIG_DB_PATH, (err) => {
    if (err) {
        console.error('Config database connection failed:', err.message);
    } else {
        console.log('Connected to config database.');
    }
});

configDb.serialize(() => {
    configDb.run(`
        CREATE TABLE IF NOT EXISTS settings (
            key TEXT PRIMARY KEY,
            value TEXT NOT NULL,
            updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
        )
    `, (err) => { if (err) console.error('CREATE settings failed:', err.message); });

    configDb.run(`
        CREATE TABLE IF NOT EXISTS external_dirs (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            path TEXT NOT NULL,
            type TEXT NOT NULL CHECK(type IN ('card_image', 'portrait', 'other')),
            priority INTEGER DEFAULT 0,
            description TEXT,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP
        )
    `, (err) => { if (err) console.error('CREATE external_dirs failed:', err.message); });

    configDb.run(`
        INSERT OR IGNORE INTO settings (key, value) VALUES 
        ('mod_id', 'VYgo'),
        ('mod_name', '储君拓展[VYgo]'),
        ('locale_prefix', 'REGENT_PLUS_CARD_'),
        ('project_root', '${__dirname.replace(/\\\\/g, '/').replace('/Web', '')}')
    `, (err) => {
        if (err) console.error('INSERT settings failed:', err.message);
        console.log('Config database tables initialized.');
    });
});

// ===================== 卡牌数据库 =====================

const cardsDb = new sqlite3.Database(CARDS_DB_PATH, (err) => {
    if (err) {
        console.error('Cards database connection failed:', err.message);
    } else {
        console.log('Connected to cards database.');
    }
});

cardsDb.serialize(() => {
    cardsDb.run(`
        CREATE TABLE IF NOT EXISTS cards (
            id INTEGER PRIMARY KEY,
            card_id INTEGER UNIQUE NOT NULL,
            name TEXT NOT NULL,
            cn_name TEXT,
            en_name TEXT,
            types TEXT,
            description TEXT,
            atk INTEGER,
            def INTEGER,
            level INTEGER,
            attribute TEXT,
            race TEXT,
            raw_data TEXT,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
        )
    `, (err) => {
        if (err) console.error('CREATE cards failed:', err.message);
        console.log('Cards database tables initialized.');
    });
});

// ===================== 查询辅助函数 =====================

function runConfig(sql, params = []) {
    return new Promise((resolve, reject) => {
        configDb.run(sql, params, function(err) {
            if (err) reject(err);
            else resolve({ id: this.lastID, changes: this.changes });
        });
    });
}

function getConfig(sql, params = []) {
    return new Promise((resolve, reject) => {
        configDb.get(sql, params, (err, row) => {
            if (err) reject(err);
            else resolve(row);
        });
    });
}

function allConfig(sql, params = []) {
    return new Promise((resolve, reject) => {
        configDb.all(sql, params, (err, rows) => {
            if (err) reject(err);
            else resolve(rows);
        });
    });
}

function runCards(sql, params = []) {
    return new Promise((resolve, reject) => {
        cardsDb.run(sql, params, function(err) {
            if (err) reject(err);
            else resolve({ id: this.lastID, changes: this.changes });
        });
    });
}

function getCards(sql, params = []) {
    return new Promise((resolve, reject) => {
        cardsDb.get(sql, params, (err, row) => {
            if (err) reject(err);
            else resolve(row);
        });
    });
}

function allCards(sql, params = []) {
    return new Promise((resolve, reject) => {
        cardsDb.all(sql, params, (err, rows) => {
            if (err) reject(err);
            else resolve(rows);
        });
    });
}

module.exports = {
    configDb,
    cardsDb,
    runConfig,
    getConfig,
    allConfig,
    runCards,
    getCards,
    allCards
};
