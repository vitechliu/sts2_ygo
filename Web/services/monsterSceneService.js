const IMAGE_NODE_NAME = 'Image';
const IMAGE_NODE_TYPE = 'Sprite2D';
const LINEAR_MIPMAP_FILTER = 4;

function isMonsterImageNodeHeader(line) {
    if (!/^\s*\[node\s/.test(line)) return false;

    const name = line.match(/(?:^|\s)name="([^"]+)"/);
    const type = line.match(/(?:^|\s)type="([^"]+)"/);
    return name?.[1] === IMAGE_NODE_NAME && type?.[1] === IMAGE_NODE_TYPE;
}

/**
 * 确保怪兽场景的 Image Sprite2D 节点使用 Linear Mipmap 纹理过滤。
 * 返回新内容及变更信息，便于批处理脚本安全地跳过未变化文件。
 */
function ensureMonsterImageTextureFilter(content) {
    const lines = content.split(/\r?\n/);
    let matchedNodes = 0;
    let changed = false;

    for (let index = 0; index < lines.length; index += 1) {
        if (!isMonsterImageNodeHeader(lines[index])) continue;

        matchedNodes += 1;
        let sectionEnd = index + 1;
        while (sectionEnd < lines.length && !/^\s*\[/.test(lines[sectionEnd])) {
            sectionEnd += 1;
        }

        const filterIndexes = [];
        for (let propertyIndex = index + 1; propertyIndex < sectionEnd; propertyIndex += 1) {
            if (/^\s*texture_filter\s*=/.test(lines[propertyIndex])) {
                filterIndexes.push(propertyIndex);
            }
        }

        if (filterIndexes.length === 0) {
            lines.splice(index + 1, 0, `texture_filter = ${LINEAR_MIPMAP_FILTER}`);
            sectionEnd += 1;
            changed = true;
        } else {
            const firstFilterIndex = filterIndexes[0];
            const indentation = lines[firstFilterIndex].match(/^\s*/)?.[0] ?? '';
            const expectedLine = `${indentation}texture_filter = ${LINEAR_MIPMAP_FILTER}`;
            if (lines[firstFilterIndex] !== expectedLine) {
                lines[firstFilterIndex] = expectedLine;
                changed = true;
            }

            for (let duplicateIndex = filterIndexes.length - 1; duplicateIndex >= 1; duplicateIndex -= 1) {
                lines.splice(filterIndexes[duplicateIndex], 1);
                sectionEnd -= 1;
                changed = true;
            }
        }

        index = sectionEnd - 1;
    }

    const newline = content.includes('\r\n') ? '\r\n' : '\n';
    return {
        content: lines.join(newline),
        matchedNodes,
        changed
    };
}

module.exports = {
    LINEAR_MIPMAP_FILTER,
    ensureMonsterImageTextureFilter
};
