#!/usr/bin/env node

const fs = require('fs');
const path = require('path');
const { ensureMonsterImageTextureFilter } = require('../services/monsterSceneService');

const sceneRoot = path.join(__dirname, '..', '..', 'VYgo', 'scenes', 'monsters');

function collectScenePaths(directory) {
    return fs.readdirSync(directory, { withFileTypes: true })
        .flatMap(entry => {
            const entryPath = path.join(directory, entry.name);
            if (entry.isDirectory()) return collectScenePaths(entryPath);
            return entry.isFile() && entry.name.endsWith('.tscn') ? [entryPath] : [];
        })
        .sort();
}

function updateScene(scenePath) {
    const originalContent = fs.readFileSync(scenePath, 'utf8');
    const result = ensureMonsterImageTextureFilter(originalContent);
    if (result.changed) {
        fs.writeFileSync(scenePath, result.content, 'utf8');
    }
    return result;
}

function main() {
    if (!fs.existsSync(sceneRoot)) {
        throw new Error(`怪兽场景目录不存在：${sceneRoot}`);
    }

    const scenePaths = collectScenePaths(sceneRoot);
    let changedFiles = 0;
    let matchedNodes = 0;
    const missingImageNodes = [];

    for (const scenePath of scenePaths) {
        const result = updateScene(scenePath);
        if (result.changed) changedFiles += 1;
        matchedNodes += result.matchedNodes;
        if (result.matchedNodes === 0) {
            missingImageNodes.push(path.relative(sceneRoot, scenePath));
        }
    }

    console.log(`已检查 ${scenePaths.length} 个怪兽场景，更新 ${changedFiles} 个文件，共处理 ${matchedNodes} 个 Image Sprite2D 节点。`);
    if (missingImageNodes.length > 0) {
        console.warn(`以下场景没有 Image Sprite2D 节点：${missingImageNodes.join(', ')}`);
    }
}

if (require.main === module) {
    main();
}

module.exports = { collectScenePaths, updateScene };
