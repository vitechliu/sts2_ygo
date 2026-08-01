const test = require('node:test');
const assert = require('node:assert/strict');
const {
    ValidationError,
    parseCardId,
    parseDatabaseId,
    normalizePriority,
    normalizeCropParams
} = require('../services/inputValidation');

test('accepts valid integer identifiers and rejects traversal or unsafe values', () => {
    assert.equal(parseCardId('70095154'), 70095154);
    assert.equal(parseDatabaseId('12'), 12);
    assert.throws(() => parseCardId('../secret'), ValidationError);
    assert.throws(() => parseCardId('1.5'), ValidationError);
    assert.throws(() => parseCardId('0'), ValidationError);
    assert.throws(() => parseDatabaseId('-1'), ValidationError);
});

test('normalizes priorities and validates crop dimensions', () => {
    assert.equal(normalizePriority('5'), 5);
    assert.equal(normalizePriority(''), 0);
    assert.throws(() => normalizePriority('1.5'), ValidationError);

    assert.deepEqual(normalizeCropParams({
        x: 1,
        y: 2,
        width: 30,
        height: 40,
        sourceWidth: 100,
        sourceHeight: 200
    }), {
        x: 1,
        y: 2,
        width: 30,
        height: 40,
        sourceWidth: 100,
        sourceHeight: 200
    });
    assert.throws(() => normalizeCropParams({
        x: 0,
        y: 0,
        width: 10,
        height: 10,
        sourceWidth: 0,
        sourceHeight: 10
    }), ValidationError);
});
