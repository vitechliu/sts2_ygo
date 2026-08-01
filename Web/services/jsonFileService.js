const fs = require('fs');
const path = require('path');

function readJsonArray(filePath) {
    return readJson(filePath, [], Array.isArray, 'an array');
}

function readJsonObject(filePath) {
    return readJson(
        filePath,
        {},
        value => value !== null && typeof value === 'object' && !Array.isArray(value),
        'an object'
    );
}

function readJson(filePath, fallback, validator, expectedType) {
    if (!fs.existsSync(filePath)) return fallback;

    let value;
    try {
        value = JSON.parse(fs.readFileSync(filePath, 'utf8'));
    } catch (error) {
        throw new Error(`Invalid JSON in ${filePath}: ${error.message}`);
    }
    if (!validator(value)) {
        throw new Error(`Invalid JSON in ${filePath}: expected ${expectedType}`);
    }
    return value;
}

function writeJsonAtomic(filePath, value) {
    fs.mkdirSync(path.dirname(filePath), { recursive: true });
    const temporaryPath = `${filePath}.${process.pid}.${Date.now()}.tmp`;
    try {
        fs.writeFileSync(temporaryPath, JSON.stringify(value, null, 4), { encoding: 'utf8', flag: 'wx' });
        fs.renameSync(temporaryPath, filePath);
    } finally {
        if (fs.existsSync(temporaryPath)) {
            fs.unlinkSync(temporaryPath);
        }
    }
}

module.exports = {
    readJsonArray,
    readJsonObject,
    writeJsonAtomic
};
