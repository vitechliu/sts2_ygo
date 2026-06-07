const fs = require('fs');
const path = require('path');
const sharp = require('sharp');
const { all } = require('../database');

class ImageService {
    constructor() {
        this.projectRoot = path.join(__dirname, '..', '..');
    }

    async findImageFile(cardId) {
        const dirs = await all(
            'SELECT * FROM external_dirs WHERE type = ? ORDER BY priority DESC',
            ['card_image']
        );

        for (const dir of dirs) {
            const dirPath = dir.path;
            if (!fs.existsSync(dirPath)) continue;

            const extensions = ['.png', '.jpg', '.jpeg', '.webp'];
            for (const ext of extensions) {
                const filePath = path.join(dirPath, `${cardId}${ext}`);
                if (fs.existsSync(filePath)) {
                    return filePath;
                }
            }
        }

        return null;
    }

    async findPortraitFile(cardId) {
        const dirs = await all(
            'SELECT * FROM external_dirs WHERE type = ? ORDER BY priority DESC',
            ['portrait']
        );

        for (const dir of dirs) {
            const dirPath = dir.path;
            if (!fs.existsSync(dirPath)) continue;

            const extensions = ['.png', '.jpg', '.jpeg', '.webp'];
            for (const ext of extensions) {
                const filePath = path.join(dirPath, `${cardId}${ext}`);
                if (fs.existsSync(filePath)) {
                    return filePath;
                }
            }
        }

        return null;
    }

    async copyPortrait(sourcePath, cardId) {
        const targetDir = path.join(this.projectRoot, 'VYgo', 'images', 'monster');
        
        if (!fs.existsSync(targetDir)) {
            fs.mkdirSync(targetDir, { recursive: true });
        }

        const targetPath = path.join(targetDir, `${cardId}.png`);
        await sharp(sourcePath).png().toFile(targetPath);
        
        return targetPath;
    }

    async processCroppedImage(sourcePath, cardId, cropParams) {
        const targetDir = path.join(this.projectRoot, 'VYgo', 'images', 'cards');
        
        if (!fs.existsSync(targetDir)) {
            fs.mkdirSync(targetDir, { recursive: true });
        }

        const targetPath = path.join(targetDir, `${cardId}.png`);

        let image = sharp(sourcePath);

        // 如果提供了裁剪参数
        if (cropParams) {
            const { x, y, width, height, sourceWidth, sourceHeight } = cropParams;
            
            // 先获取原图信息
            const metadata = await sharp(sourcePath).metadata();
            const actualWidth = metadata.width;
            const actualHeight = metadata.height;
            
            // 计算缩放比例
            const scaleX = actualWidth / sourceWidth;
            const scaleY = actualHeight / sourceHeight;
            
            // 计算实际裁剪坐标
            const actualX = Math.round(x * scaleX);
            const actualY = Math.round(y * scaleY);
            const actualCropWidth = Math.round(width * scaleX);
            const actualCropHeight = Math.round(height * scaleY);
            
            // 确保不超出边界
            const safeX = Math.max(0, Math.min(actualX, actualWidth - 1));
            const safeY = Math.max(0, Math.min(actualY, actualHeight - 1));
            const safeWidth = Math.min(actualCropWidth, actualWidth - safeX);
            const safeHeight = Math.min(actualCropHeight, actualHeight - safeY);

            image = image.extract({
                left: safeX,
                top: safeY,
                width: safeWidth,
                height: safeHeight
            });

            // 缩放到目标尺寸
            image = image.resize(1000, 760, {
                fit: 'fill'
            });
        } else {
            // 无裁剪参数，直接缩放
            image = image.resize(1000, 760, {
                fit: 'cover',
                position: 'center'
            });
        }

        await image.png().toFile(targetPath);
        
        return targetPath;
    }

    async downloadImage(url, cardId) {
        const axios = require('axios');
        const targetDir = path.join(this.projectRoot, 'VYgo', 'images', 'cards');
        
        if (!fs.existsSync(targetDir)) {
            fs.mkdirSync(targetDir, { recursive: true });
        }

        const targetPath = path.join(targetDir, `${cardId}.png`);

        try {
            const response = await axios({
                method: 'GET',
                url: url,
                responseType: 'stream',
                timeout: 30000
            });

            const writer = fs.createWriteStream(targetPath);
            response.data.pipe(writer);

            await new Promise((resolve, reject) => {
                writer.on('finish', resolve);
                writer.on('error', reject);
            });

            return targetPath;
        } catch (error) {
            throw new Error(`Download failed: ${error.message}`);
        }
    }

    getImagePath(cardId) {
        return path.join(this.projectRoot, 'VYgo', 'images', 'cards', `${cardId}.png`);
    }

    getPortraitPath(cardId) {
        return path.join(this.projectRoot, 'VYgo', 'images', 'monster', `${cardId}.png`);
    }

    async imageExists(cardId) {
        const imagePath = this.getImagePath(cardId);
        return fs.existsSync(imagePath);
    }

    async portraitExists(cardId) {
        const portraitPath = this.getPortraitPath(cardId);
        return fs.existsSync(portraitPath);
    }
}

module.exports = new ImageService();
