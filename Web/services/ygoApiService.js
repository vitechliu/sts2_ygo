const axios = require('axios');

const BASE_URL = 'https://ygocdb.com';

class YgoApiService {
    async getCardById(cardId) {
        try {
            const response = await axios.get(`${BASE_URL}/api/v0/card/${cardId}?show=all`, {
                timeout: 10000,
                maxContentLength: 2 * 1024 * 1024
            });
            return response.data;
        } catch (error) {
            if (error.response?.status === 404) {
                throw new Error(`Card ${cardId} not found`);
            }
            throw new Error(`API request failed: ${error.message}`);
        }
    }

    parseCardData(apiData) {
        const data = apiData.data || {};
        const text = apiData.text || {};
        
        // 提取英文名并去除非英文数字符号
        const rawEnName = String(apiData.en_name || text.en_name || '');
        const cleanEnName = rawEnName.replace(/[^a-zA-Z0-9]/g, '');
        
        return {
            cardId: apiData.id,
            name: text.name || '',
            cnName: apiData.nwbbs_n || apiData.cn_name || text.name || '',
            enName: cleanEnName,
            rawEnName: rawEnName,
            types: text.types || '',
            description: text.desc || '',
            atk: data.atk || 0,
            def: data.def || 0,
            level: data.level || 0,
            attribute: this.parseAttribute(data.attribute),
            race: this.parseRace(data.race),
            rawData: this.sanitizeRawData(apiData)
        };
    }

    sanitizeRawData(rawData) {
        const data = typeof rawData === 'string' ? JSON.parse(rawData) : rawData;
        if (data === null || typeof data !== 'object' || Array.isArray(data)) {
            throw new Error('Raw card data must be a JSON object');
        }
        const sanitized = { ...data };

        delete sanitized.faqs;
        delete sanitized.faqcount;
        delete sanitized.jppacks;
        delete sanitized.enpacks;

        return JSON.stringify(sanitized);
    }

    parseAttribute(attr) {
        const attrs = {
            1: '地', 2: '水', 4: '炎', 8: '风',
            16: '光', 32: '暗', 64: '神'
        };
        return attrs[attr] || '';
    }

    parseRace(race) {
        const races = {
            1: '战士族', 2: '魔法师族', 4: '天使族', 8: '恶魔族',
            16: '不死族', 32: '机械族', 64: '水族', 128: '炎族',
            256: '岩石族', 512: '鸟兽族', 1024: '植物族', 2048: '昆虫族',
            4096: '雷族', 8192: '龙族', 16384: '兽族', 32768: '兽战士族',
            65536: '恐龙族', 131072: '鱼族', 262144: '海龙族', 524288: '爬虫类族',
            1048576: '念动力族', 2097152: '幻神兽族', 4194304: '创造神族', 8388608: '幻龙族',
            16777216: '电子界族'
        };
        return races[race] || '';
    }
}

module.exports = new YgoApiService();
