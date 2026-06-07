const sqlite3 = require('sqlite3').verbose();
const path = require('path');
const fs = require('fs');

const DATA_DIR = path.join(__dirname, 'data');
const DB_PATH = path.join(DATA_DIR, 'app.db');

if (!fs.existsSync(DATA_DIR)) {
    fs.mkdirSync(DATA_DIR, { recursive: true });
}

const db = new sqlite3.Database(DB_PATH, (err) => {
    if (err) {
        console.error('Database connection failed:', err.message);
    } else {
        console.log('Connected to SQLite database.');
        initTables();
    }
});

function initTables() {
    db.serialize(() => {
        // 全局设置表
        db.run(`
            CREATE TABLE IF NOT EXISTS settings (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL,
                updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )
        `);

        // 外部资源目录表
        db.run(`
            CREATE TABLE IF NOT EXISTS external_dirs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                path TEXT NOT NULL,
                type TEXT NOT NULL CHECK(type IN ('card_image', 'portrait', 'other')),
                priority INTEGER DEFAULT 0,
                description TEXT,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )
        `);

        // 卡牌数据表
        db.run(`
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
                image_path TEXT,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )
        `);

        // 插入默认设置
        db.run(`
            INSERT OR IGNORE INTO settings (key, value) VALUES 
            ('mod_id', 'VYgo'),
            ('mod_name', '储君拓展[VYgo]'),
            ('locale_prefix', 'REGENT_PLUS_CARD_'),
            ('project_root', '${__dirname.replace(/\\/g, '/').replace('/Web', '')}')
        `);

        console.log('Database tables initialized.');
    });
}

function run(sql, params = []) {
    return new Promise((resolve, reject) => {
        db.run(sql, params, function(err) {
            if (err) reject(err);
            else resolve({ id: this.lastID, changes: this.changes });
        });
    });
}

function get(sql, params = []) {
    return new Promise((resolve, reject) => {
        db.get(sql, params, (err, row) => {
            if (err) reject(err);
            else resolve(row);
        });
    });
}

function all(sql, params = []) {
    return new Promise((resolve, reject) => {
        db.all(sql, params, (err, rows) => {
            if (err) reject(err);
            else resolve(rows);
        });
    });
}

module.exports = {
    db,
    run,
    get,
    all
};
