const fs = require('fs');
const path = require('path');
const sqlite3 = require('sqlite3').verbose();

const projectRoot = path.join(__dirname, '..', '..');
const defaultStringsPath = path.join(projectRoot, 'External', 'ygopro', 'strings.conf');
const defaultDatabasePath = path.join(projectRoot, 'External', 'ygopro', 'cards.cdb');

function parseSetnameContent(content) {
    const archetypes = new Map();

    for (const line of content.split(/\r?\n/)) {
        const match = line.match(/^!setname\s+(0x[0-9a-fA-F]+)\s+([^\t]*?)(?:\t(.*))?$/);
        if (!match) continue;

        const code = Number.parseInt(match[1], 16);
        if (!Number.isInteger(code) || code <= 0 || code > 0xffff) continue;

        archetypes.set(code, {
            code,
            hex: formatArchetypeCode(code),
            cnName: match[2].trim(),
            jaName: (match[3] || '').trim()
        });
    }

    return archetypes;
}

function decodeSetcodeHex(value) {
    const normalized = String(value || '').replace(/^0x/i, '').padStart(16, '0');
    if (!/^[0-9a-fA-F]{16}$/.test(normalized)) {
        throw new Error(`Invalid setcode hex value: ${value}`);
    }

    const codes = [];
    for (let offset = normalized.length - 4; offset >= 0; offset -= 4) {
        const code = Number.parseInt(normalized.slice(offset, offset + 4), 16);
        if (code !== 0) codes.push(code);
    }
    return codes;
}

function expandArchetypeCodes(exactCodes, metadata) {
    const expanded = new Set();

    for (const code of exactCodes) {
        expanded.add(code);
        const parentCode = code & 0x0fff;
        if (parentCode !== code && metadata.has(parentCode)) {
            expanded.add(parentCode);
        }
    }

    return [...expanded].sort((left, right) => left - right);
}

function buildArchetypeResult(setcodeHex, metadata) {
    const exactCodes = decodeSetcodeHex(setcodeHex);
    const codes = expandArchetypeCodes(exactCodes, metadata);
    const unknownCodes = exactCodes.filter(code => !metadata.has(code));
    const warning = unknownCodes.length > 0
        ? `Unknown archetype codes: ${unknownCodes.map(formatArchetypeCode).join(', ')}`
        : null;

    return {
        codes,
        archetypes: codes.map(code => metadata.get(code) || {
            code,
            hex: formatArchetypeCode(code),
            cnName: '',
            jaName: ''
        }),
        warning
    };
}

function formatArchetypeCode(code) {
    return `0x${Number(code).toString(16).padStart(4, '0').toUpperCase()}`;
}

class ArchetypeService {
    constructor(options = {}) {
        this.stringsPath = options.stringsPath || defaultStringsPath;
        this.databasePath = options.databasePath || defaultDatabasePath;
    }

    async getCardsArchetypes(cardIds) {
        const ids = [...new Set(cardIds.map(Number).filter(Number.isInteger))];
        const results = new Map();
        if (ids.length === 0) return results;

        let metadata;
        try {
            metadata = parseSetnameContent(fs.readFileSync(this.stringsPath, 'utf8'));
            if (metadata.size === 0) {
                throw new Error('No active !setname entries found');
            }
        } catch (error) {
            return this.createFailureResults(ids, `Unable to load strings.conf: ${error.message}`);
        }

        let rows;
        try {
            rows = await this.querySetcodes(ids);
        } catch (error) {
            return this.createFailureResults(ids, `Unable to query cards.cdb: ${error.message}`);
        }

        const rowsById = new Map(rows.map(row => [Number(row.id), row]));
        for (const id of ids) {
            const row = rowsById.get(id);
            if (!row) {
                results.set(id, emptyArchetypeResult(`Card ${id} was not found in cards.cdb`));
                continue;
            }

            try {
                results.set(id, buildArchetypeResult(row.setcode_hex, metadata));
            } catch (error) {
                results.set(id, emptyArchetypeResult(`Unable to decode archetypes for card ${id}: ${error.message}`));
            }
        }

        return results;
    }

    createFailureResults(ids, warning) {
        return new Map(ids.map(id => [id, emptyArchetypeResult(warning)]));
    }

    async querySetcodes(ids) {
        const db = await openReadonlyDatabase(this.databasePath);
        try {
            const rows = [];
            const chunkSize = 900;
            for (let offset = 0; offset < ids.length; offset += chunkSize) {
                const chunk = ids.slice(offset, offset + chunkSize);
                const placeholders = chunk.map(() => '?').join(', ');
                const chunkRows = await all(
                    db,
                    `SELECT id, printf('%016x', setcode) AS setcode_hex FROM datas WHERE id IN (${placeholders})`,
                    chunk
                );
                rows.push(...chunkRows);
            }
            return rows;
        } finally {
            await close(db);
        }
    }
}

function emptyArchetypeResult(warning = null) {
    return { codes: [], archetypes: [], warning };
}

function openReadonlyDatabase(databasePath) {
    return new Promise((resolve, reject) => {
        const db = new sqlite3.Database(databasePath, sqlite3.OPEN_READONLY, error => {
            if (error) reject(error);
            else resolve(db);
        });
    });
}

function all(db, sql, params) {
    return new Promise((resolve, reject) => {
        db.all(sql, params, (error, rows) => {
            if (error) reject(error);
            else resolve(rows);
        });
    });
}

function close(db) {
    return new Promise((resolve, reject) => {
        db.close(error => {
            if (error) reject(error);
            else resolve();
        });
    });
}

module.exports = new ArchetypeService();
module.exports.ArchetypeService = ArchetypeService;
module.exports.parseSetnameContent = parseSetnameContent;
module.exports.decodeSetcodeHex = decodeSetcodeHex;
module.exports.expandArchetypeCodes = expandArchetypeCodes;
module.exports.buildArchetypeResult = buildArchetypeResult;
module.exports.formatArchetypeCode = formatArchetypeCode;

