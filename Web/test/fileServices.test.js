const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs');
const os = require('os');
const path = require('path');
const { isFileWithinDirectory, isSupportedImagePath } = require('../services/pathService');
const { readJsonArray, readJsonObject, writeJsonAtomic } = require('../services/jsonFileService');

test('allows supported images only when they are inside the configured directory', t => {
    const root = fs.mkdtempSync(path.join(os.tmpdir(), 'vygo-path-test-'));
    t.after(() => fs.rmSync(root, { recursive: true, force: true }));
    const allowedDir = path.join(root, 'allowed');
    const outsideDir = path.join(root, 'outside');
    fs.mkdirSync(allowedDir);
    fs.mkdirSync(outsideDir);
    const allowedImage = path.join(allowedDir, '1.PNG');
    const outsideImage = path.join(outsideDir, '2.png');
    fs.writeFileSync(allowedImage, 'image');
    fs.writeFileSync(outsideImage, 'image');

    assert.equal(isSupportedImagePath(allowedImage), true);
    assert.equal(isFileWithinDirectory(allowedImage, allowedDir), true);
    assert.equal(isFileWithinDirectory(outsideImage, allowedDir), false);
    assert.equal(isSupportedImagePath(path.join(allowedDir, 'notes.txt')), false);
});

test('writes JSON atomically and refuses malformed or unexpected data', t => {
    const root = fs.mkdtempSync(path.join(os.tmpdir(), 'vygo-json-test-'));
    t.after(() => fs.rmSync(root, { recursive: true, force: true }));
    const filePath = path.join(root, 'data.json');

    writeJsonAtomic(filePath, [{ card_id: 1 }]);
    assert.deepEqual(readJsonArray(filePath), [{ card_id: 1 }]);
    writeJsonAtomic(filePath, [{ card_id: 2 }]);
    assert.deepEqual(readJsonArray(filePath), [{ card_id: 2 }]);
    assert.throws(() => readJsonObject(filePath), /expected an object/);

    fs.writeFileSync(filePath, '{broken');
    assert.throws(() => readJsonArray(filePath), /Invalid JSON/);
});
