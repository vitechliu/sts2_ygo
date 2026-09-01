const test = require('node:test');
const assert = require('node:assert/strict');
const { ensureMonsterImageTextureFilter } = require('../services/monsterSceneService');

test('adds Linear Mipmap to the Image Sprite2D node only', () => {
    const source = `[node name="Visuals" type="Node2D" parent="."]
texture_filter = 1

[node name="Image" type="Sprite2D" parent="Visuals"]
position = Vector2(1, 2)

[node name="Other" type="Sprite2D" parent="Visuals"]
texture_filter = 2
`;

    const result = ensureMonsterImageTextureFilter(source);

    assert.equal(result.matchedNodes, 1);
    assert.equal(result.changed, true);
    assert.match(result.content, /\[node name="Image" type="Sprite2D" parent="Visuals"\]\ntexture_filter = 4\nposition/);
    assert.match(result.content, /\[node name="Visuals"[^\n]+\]\ntexture_filter = 1/);
    assert.match(result.content, /\[node name="Other"[^\n]+\]\ntexture_filter = 2/);
});

test('replaces another filter value and is idempotent', () => {
    const source = `[node name="Image" type="Sprite2D" parent="Visuals"]\r
texture_filter = 2\r
texture = ExtResource("1")\r
`;

    const firstResult = ensureMonsterImageTextureFilter(source);
    const secondResult = ensureMonsterImageTextureFilter(firstResult.content);

    assert.equal(firstResult.content.includes('\r\n'), true);
    assert.equal(firstResult.content.match(/texture_filter = 4/g)?.length, 1);
    assert.equal(secondResult.changed, false);
    assert.equal(secondResult.content, firstResult.content);
});
