const test = require('node:test');
const assert = require('node:assert/strict');
const path = require('path');
const {
    ArchetypeService,
    parseSetnameContent,
    decodeSetcodeHex,
    buildArchetypeResult
} = require('../services/archetypeService');
const { toExportCardData, mergeExportedCard } = require('../services/cardExportService');

test('parses only active setname entries and preserves translated names', () => {
    const metadata = parseSetnameContent([
        '#setname 0x92 被注释\tコメント',
        '!setname 0x93 电子\tサイバー',
        '!setname 0x11e 未界域',
        '!setname 0x135 @火灵天星\t@イグニスター',
        '!system 1 通常召唤'
    ].join('\n'));

    assert.equal(metadata.size, 3);
    assert.deepEqual(metadata.get(0x93), {
        code: 0x93,
        hex: '0x0093',
        cnName: '电子',
        jaName: 'サイバー'
    });
    assert.equal(metadata.get(0x11e).jaName, '');
    assert.equal(metadata.get(0x135).cnName, '@火灵天星');
});

test('decodes all four 16-bit fields without converting through a JS number', () => {
    assert.deepEqual(decodeSetcodeHex('0000019401843008'), [0x3008, 0x0184, 0x0194]);
    assert.deepEqual(decodeSetcodeHex('0'), []);
});

test('expands an exact child code to an active parent and sorts the result', () => {
    const metadata = parseSetnameContent([
        '!setname 0x93 电子\tサイバー',
        '!setname 0x1093 电子龙\tサイバー・ドラゴン'
    ].join('\n'));

    const result = buildArchetypeResult('0000000000001093', metadata);
    assert.deepEqual(result.codes, [0x93, 0x1093]);
    assert.deepEqual(result.archetypes.map(item => item.cnName), ['电子', '电子龙']);
    assert.equal(result.warning, null);
});

test('keeps an unknown exact code but reports its missing translation', () => {
    const result = buildArchetypeResult('0000000000001999', new Map());
    assert.deepEqual(result.codes, [0x1999]);
    assert.match(result.warning, /0x1999/);
});

test('reads expected Cyber archetypes from the pinned upstream database', async () => {
    const service = new ArchetypeService();
    const results = await service.getCardsArchetypes([70095154, 68774379]);

    assert.deepEqual(results.get(70095154).codes, [0x93, 0x1093]);
    assert.deepEqual(results.get(68774379).codes, [0x93]);
});

test('returns warnings and empty arrays for missing sources and card ids', async () => {
    const missingSource = new ArchetypeService({
        stringsPath: path.join(__dirname, 'missing-strings.conf'),
        databasePath: path.join(__dirname, 'missing-cards.cdb')
    });
    const sourceResult = await missingSource.getCardsArchetypes([70095154]);
    assert.deepEqual(sourceResult.get(70095154).codes, []);
    assert.match(sourceResult.get(70095154).warning, /strings\.conf/);

    const service = new ArchetypeService();
    const missingCardResult = await service.getCardsArchetypes([999999999]);
    assert.deepEqual(missingCardResult.get(999999999).codes, []);
    assert.match(missingCardResult.get(999999999).warning, /not found/);
});

test('single and full export data replace stale archetype arrays', () => {
    const card = {
        id: 1,
        card_id: 70095154,
        name: '电子龙',
        archetypes: [999],
        raw_data: '{}',
        created_at: 'old',
        updated_at: 'old'
    };
    const exported = toExportCardData(card, [0x93, 0x1093]);
    assert.deepEqual(exported.archetypes, [0x93, 0x1093]);
    assert.equal('raw_data' in exported, false);

    const merged = mergeExportedCard([{ card_id: 70095154, archetypes: [999] }], exported);
    assert.equal(merged.updated, true);
    assert.deepEqual(merged.cards[0].archetypes, [0x93, 0x1093]);
});

