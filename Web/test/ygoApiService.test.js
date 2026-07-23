const test = require('node:test');
const assert = require('node:assert/strict');
const { YgoApiService, normalizeCardName } = require('../services/ygoApiService');

test('normalizes punctuation and whitespace when matching card names', () => {
    assert.equal(normalizeCardName(' 电子龙 · 无限 '), normalizeCardName('电子龙无限'));
    assert.equal(normalizeCardName('Cyber Dragon'), normalizeCardName('cyber-dragon'));
});

test('parses ygocdb search results and puts exact matches first', () => {
    const service = new YgoApiService();
    const results = service.parseSearchResults({
        result: [
            {
                id: 23893227,
                nwbbs_n: '电子龙核',
                en_name: 'Cyber Dragon Core',
                text: { types: '[怪兽|效果] 机械/光' },
                data: { atk: 400, def: 1500, level: 2 }
            },
            {
                id: 70095154,
                nwbbs_n: '电子龙',
                en_name: 'Cyber Dragon',
                jp_name: 'サイバー・ドラゴン',
                text: { types: '[怪兽|效果] 机械/光' },
                data: { atk: 2100, def: 1600, level: 5 }
            }
        ]
    }, '电子龙');

    assert.equal(results.length, 2);
    assert.deepEqual(results[0], {
        cardId: 70095154,
        cnName: '电子龙',
        enName: 'Cyber Dragon',
        jpName: 'サイバー・ドラゴン',
        types: '[怪兽|效果] 机械/光',
        atk: 2100,
        def: 1600,
        level: 5,
        isExactMatch: true,
        matchedName: '电子龙'
    });
    assert.equal(results[1].isExactMatch, false);
});

test('ignores malformed search entries', () => {
    const service = new YgoApiService();
    const results = service.parseSearchResults({
        result: [{ id: null }, { id: 'not-a-number' }, { id: 0 }]
    }, '电子龙');

    assert.deepEqual(results, []);
});
