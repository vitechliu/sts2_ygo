const fs = require('fs');
const path = require('path');

const IMAGE_EXTENSIONS = new Set(['.png', '.jpg', '.jpeg', '.webp']);

function isSupportedImagePath(filePath) {
    return typeof filePath === 'string'
        && IMAGE_EXTENSIONS.has(path.extname(filePath).toLowerCase());
}

function isFileWithinDirectory(filePath, directoryPath) {
    try {
        const realFile = fs.realpathSync(filePath);
        const realDirectory = fs.realpathSync(directoryPath);
        if (!fs.statSync(realFile).isFile() || !fs.statSync(realDirectory).isDirectory()) {
            return false;
        }

        const relative = path.relative(realDirectory, realFile);
        return relative !== '' && !relative.startsWith(`..${path.sep}`) && relative !== '..' && !path.isAbsolute(relative);
    } catch {
        return false;
    }
}

module.exports = {
    IMAGE_EXTENSIONS,
    isSupportedImagePath,
    isFileWithinDirectory
};
