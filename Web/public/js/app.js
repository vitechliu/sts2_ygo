// API 基础地址
const API_BASE = '/api';

// 全局状态
let currentCardData = null;
let cropState = {
    canvas: null,
    ctx: null,
    image: null,
    scale: 1,
    offsetX: 0,
    offsetY: 0,
    isDragging: false,
    lastX: 0,
    lastY: 0,
    cropWidth: 500,   // 裁剪框在canvas上的显示宽度
    cropHeight: 380   // 裁剪框在canvas上的显示高度（1000x760的一半）
};

// 页面加载完成后初始化
document.addEventListener('DOMContentLoaded', () => {
    initTabs();
    initCardSearch();
    initConfig();
    initCropCanvas();
    loadCards();
});

// 标签页切换
function initTabs() {
    const navBtns = document.querySelectorAll('.nav-btn');
    const tabContents = document.querySelectorAll('.tab-content');

    navBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            const tab = btn.dataset.tab;

            navBtns.forEach(b => b.classList.remove('active'));
            tabContents.forEach(c => c.classList.remove('active'));

            btn.classList.add('active');
            document.getElementById(`${tab}-tab`).classList.add('active');

            if (tab === 'config') {
                loadExternalDirs();
                loadSettings();
            }
        });
    });
}

// ===================== 卡牌查询 =====================

function initCardSearch() {
    const queryBtn = document.getElementById('queryBtn');
    const cardIdInput = document.getElementById('cardIdInput');

    queryBtn.addEventListener('click', queryCard);
    cardIdInput.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') queryCard();
    });

    document.getElementById('genLocaleBtn').addEventListener('click', generateLocalizationEntry);
    document.getElementById('resetCropBtn').addEventListener('click', resetCropView);
    document.getElementById('generateCardBtn').addEventListener('click', generateCard);
    document.getElementById('refreshBtn').addEventListener('click', loadCards);
    document.getElementById('generateLocaleBtn').addEventListener('click', generateFullLocalization);
}

async function queryCard() {
    const cardId = document.getElementById('cardIdInput').value.trim();
    if (!cardId) {
        showToast('请输入卡牌ID', 'warning');
        return;
    }

    try {
        const queryBtn = document.getElementById('queryBtn');
        queryBtn.disabled = true;
        queryBtn.textContent = '查询中...';

        const response = await fetch(`${API_BASE}/cards/query/${cardId}`);
        const result = await response.json();

        if (!result.success) {
            throw new Error(result.error);
        }

        currentCardData = result.data;
        displayQueryResult(result.data);
    } catch (error) {
        showToast(error.message, 'error');
        document.getElementById('queryResult').classList.add('hidden');
    } finally {
        const queryBtn = document.getElementById('queryBtn');
        queryBtn.disabled = false;
        queryBtn.textContent = '查询';
    }
}

function displayQueryResult(data) {
    const resultDiv = document.getElementById('queryResult');
    resultDiv.classList.remove('hidden');

    // 1. 显示API信息
    if (data.apiData) {
        document.getElementById('infoCnName').textContent = data.apiData.cnName || '-';
        document.getElementById('infoEnName').textContent = data.apiData.rawEnName || '-';
        document.getElementById('infoCleanEnName').textContent = data.apiData.enName || '-';
        document.getElementById('infoTypes').textContent = data.apiData.types || '-';
        document.getElementById('infoDesc').textContent = data.apiData.description || '-';
        
        const stats = [];
        if (data.apiData.atk) stats.push(`ATK ${data.apiData.atk}`);
        if (data.apiData.def) stats.push(`DEF ${data.apiData.def}`);
        if (data.apiData.level) stats.push(`Lv.${data.apiData.level}`);
        document.getElementById('infoStats').textContent = stats.join(' / ') || '-';

        // 启用/禁用本地化按钮
        const localeBtn = document.getElementById('genLocaleBtn');
        if (data.apiData.enName && data.apiData.enName.length > 0) {
            localeBtn.disabled = false;
            localeBtn.textContent = '生成本地化';
        } else {
            localeBtn.disabled = true;
            localeBtn.textContent = '无英文名，无法生成';
        }
    } else {
        document.getElementById('infoCnName').textContent = 'API查询失败';
        document.getElementById('infoEnName').textContent = '-';
        document.getElementById('infoCleanEnName').textContent = '-';
        document.getElementById('infoTypes').textContent = '-';
        document.getElementById('infoDesc').textContent = '-';
        document.getElementById('infoStats').textContent = '-';
        document.getElementById('genLocaleBtn').disabled = true;
    }

    // 2. 显示卡图/裁剪区域
    const cropContainer = document.getElementById('cropContainer');
    const noCardImage = document.getElementById('noCardImage');
    const cropStatus = document.getElementById('cropStatus');
    const generateBtn = document.getElementById('generateCardBtn');

    if (data.cardImage && data.cardImage.found) {
        cropContainer.classList.remove('hidden');
        noCardImage.classList.add('hidden');
        cropStatus.textContent = data.cardImage.hasLocal ? '本地已存在' : '已找到';
        cropStatus.className = 'status-badge status-ok';
        generateBtn.disabled = false;
        
        // 加载图片到裁剪画布
        loadImageToCrop(data.cardImage.path);
    } else {
        cropContainer.classList.add('hidden');
        noCardImage.classList.remove('hidden');
        cropStatus.textContent = '未找到';
        cropStatus.className = 'status-badge status-error';
        generateBtn.disabled = true;
    }

    // 3. 显示立绘
    const portraitPreview = document.getElementById('portraitPreview');
    const noPortrait = document.getElementById('noPortrait');
    const portraitStatus = document.getElementById('portraitStatus');

    if (data.portrait && data.portrait.found) {
        portraitPreview.classList.remove('hidden');
        noPortrait.classList.add('hidden');
        portraitStatus.textContent = data.portrait.hasLocal ? '本地已存在' : '已找到';
        portraitStatus.className = 'status-badge status-ok';
        
        // 加载立绘预览
        document.getElementById('portraitImg').src = 
            `${API_BASE}/cards/image-preview?path=${encodeURIComponent(data.portrait.path)}`;
    } else {
        portraitPreview.classList.add('hidden');
        noPortrait.classList.remove('hidden');
        portraitStatus.textContent = '未找到';
        portraitStatus.className = 'status-badge status-error';
    }
}

// ===================== 本地化 =====================

async function generateLocalizationEntry() {
    if (!currentCardData || !currentCardData.apiData) return;
    
    const { cardId, apiData } = currentCardData;
    
    try {
        const btn = document.getElementById('genLocaleBtn');
        btn.disabled = true;
        btn.textContent = '生成中...';

        const response = await fetch(`${API_BASE}/cards/localization`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                cardId,
                enName: apiData.enName,
                cnName: apiData.cnName
            })
        });

        const result = await response.json();

        if (!result.success) {
            throw new Error(result.error);
        }

        showToast('本地化条目已生成！');
        btn.textContent = '已生成';
    } catch (error) {
        showToast(error.message, 'error');
        document.getElementById('genLocaleBtn').disabled = false;
        document.getElementById('genLocaleBtn').textContent = '生成本地化';
    }
}

// ===================== 卡图裁剪 =====================

function initCropCanvas() {
    const canvas = document.getElementById('cropCanvas');
    const ctx = canvas.getContext('2d');
    
    // 设置canvas尺寸
    canvas.width = 800;
    canvas.height = 600;
    
    cropState.canvas = canvas;
    cropState.ctx = ctx;
    
    // 滚轮缩放
    canvas.addEventListener('wheel', handleCropWheel, { passive: false });
    
    // 拖拽
    canvas.addEventListener('mousedown', handleCropMouseDown);
    canvas.addEventListener('mousemove', handleCropMouseMove);
    canvas.addEventListener('mouseup', handleCropMouseUp);
    canvas.addEventListener('mouseleave', handleCropMouseUp);
}

function loadImageToCrop(imagePath) {
    const img = new Image();
    img.onload = () => {
        cropState.image = img;
        resetCropView();
    };
    img.onerror = () => {
        showToast('卡图加载失败: ' + imagePath, 'error');
    };
    // 通过后端代理接口加载，避免浏览器禁止本地资源
    img.src = `${API_BASE}/cards/image-preview?path=${encodeURIComponent(imagePath)}`;
}

function resetCropView() {
    if (!cropState.image) return;
    
    const img = cropState.image;
    const canvas = cropState.canvas;
    
    // 计算最小缩放：使图片刚好填满裁剪框，不留 gap
    const minScale = Math.max(
        cropState.cropWidth / img.width,
        cropState.cropHeight / img.height
    );
    
    cropState.scale = minScale;
    
    // 居中
    cropState.offsetX = canvas.width / 2 - img.width * cropState.scale / 2;
    cropState.offsetY = canvas.height / 2 - img.height * cropState.scale / 2;
    
    drawCropCanvas();
}

function drawCropCanvas() {
    const { canvas, ctx, image, scale, offsetX, offsetY, cropWidth, cropHeight } = cropState;
    
    if (!image) return;
    
    // 清空
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    
    // 绘制图片
    ctx.save();
    ctx.setTransform(scale, 0, 0, scale, offsetX, offsetY);
    ctx.drawImage(image, 0, 0);
    ctx.restore();
    
    // 绘制遮罩和裁剪框
    const cropX = canvas.width / 2 - cropWidth / 2;
    const cropY = canvas.height / 2 - cropHeight / 2;
    
    // 半透明遮罩
    ctx.fillStyle = 'rgba(0, 0, 0, 0.6)';
    ctx.fillRect(0, 0, canvas.width, cropY);
    ctx.fillRect(0, cropY + cropHeight, canvas.width, canvas.height - cropY - cropHeight);
    ctx.fillRect(0, cropY, cropX, cropHeight);
    ctx.fillRect(cropX + cropWidth, cropY, canvas.width - cropX - cropWidth, cropHeight);
    
    // 裁剪框边框
    ctx.strokeStyle = '#e94560';
    ctx.lineWidth = 3;
    ctx.strokeRect(cropX, cropY, cropWidth, cropHeight);
    
    // 角标
    ctx.fillStyle = '#e94560';
    const cornerSize = 10;
    // 左上
    ctx.fillRect(cropX - 1, cropY - 1, cornerSize, 3);
    ctx.fillRect(cropX - 1, cropY - 1, 3, cornerSize);
    // 右上
    ctx.fillRect(cropX + cropWidth - cornerSize + 1, cropY - 1, cornerSize, 3);
    ctx.fillRect(cropX + cropWidth - 2, cropY - 1, 3, cornerSize);
    // 左下
    ctx.fillRect(cropX - 1, cropY + cropHeight - 2, cornerSize, 3);
    ctx.fillRect(cropX - 1, cropY + cropHeight - cornerSize + 1, 3, cornerSize);
    // 右下
    ctx.fillRect(cropX + cropWidth - cornerSize + 1, cropY + cropHeight - 2, cornerSize, 3);
    ctx.fillRect(cropX + cropWidth - 2, cropY + cropHeight - cornerSize + 1, 3, cornerSize);
    
    // 尺寸提示
    ctx.fillStyle = '#fff';
    ctx.font = '12px sans-serif';
    ctx.fillText('1000 x 760', cropX + 5, cropY + 15);
}

function handleCropWheel(e) {
    e.preventDefault();
    
    const { canvas, scale, offsetX, offsetY, image } = cropState;
    const rect = canvas.getBoundingClientRect();
    const mx = e.clientX - rect.left;
    const my = e.clientY - rect.top;
    
    const delta = e.deltaY > 0 ? 0.9 : 1.1;
    const newScale = scale * delta;
    
    // 限制缩放范围
    let finalScale = newScale;
    let finalOffsetX = mx * (1 - delta) + offsetX * delta;
    let finalOffsetY = my * (1 - delta) + offsetY * delta;

    if (image) {
        const minScale = Math.max(
            cropState.cropWidth / image.width,
            cropState.cropHeight / image.height
        );
        if (newScale < minScale) {
            // 吸附到最小：刚好填满裁剪框
            finalScale = minScale;
            finalOffsetX = canvas.width / 2 - image.width * minScale / 2;
            finalOffsetY = canvas.height / 2 - image.height * minScale / 2;
        }
    }

    cropState.offsetX = finalOffsetX;
    cropState.offsetY = finalOffsetY;
    cropState.scale = finalScale;
    
    drawCropCanvas();
}

function handleCropMouseDown(e) {
    cropState.isDragging = true;
    cropState.lastX = e.clientX;
    cropState.lastY = e.clientY;
    cropState.canvas.style.cursor = 'grabbing';
}

function handleCropMouseMove(e) {
    if (!cropState.isDragging) return;
    
    const dx = e.clientX - cropState.lastX;
    const dy = e.clientY - cropState.lastY;
    
    cropState.offsetX += dx;
    cropState.offsetY += dy;
    cropState.lastX = e.clientX;
    cropState.lastY = e.clientY;
    
    drawCropCanvas();
}

function handleCropMouseUp() {
    cropState.isDragging = false;
    if (cropState.canvas) {
        cropState.canvas.style.cursor = 'grab';
    }
}

// ===================== 生成卡牌 =====================

async function generateCard() {
    if (!currentCardData || !currentCardData.apiData) {
        showToast('请先查询卡牌', 'warning');
        return;
    }

    const { cardId, apiData, cardImage, portrait } = currentCardData;

    try {
        const btn = document.getElementById('generateCardBtn');
        btn.disabled = true;
        btn.textContent = '生成中...';

        // 计算裁剪参数
        let cropParams = null;
        if (cropState.image && cardImage && cardImage.found) {
            const { scale, offsetX, offsetY, cropWidth, cropHeight, image: img } = cropState;
            
            // 裁剪框在canvas上的位置
            const canvas = cropState.canvas;
            const cropX = canvas.width / 2 - cropWidth / 2;
            const cropY = canvas.height / 2 - cropHeight / 2;
            
            // 计算原图上的裁剪坐标
            // canvas坐标 = offsetX + 原图坐标 * scale
            // 原图坐标 = (canvas坐标 - offsetX) / scale
            const x = (cropX - offsetX) / scale;
            const y = (cropY - offsetY) / scale;
            const width = cropWidth / scale;
            const height = cropHeight / scale;
            
            cropParams = {
                x: Math.max(0, x),
                y: Math.max(0, y),
                width: Math.min(width, img.width),
                height: Math.min(height, img.height),
                sourceWidth: img.width,
                sourceHeight: img.height
            };
        }

        const response = await fetch(`${API_BASE}/cards`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                cardId,
                name: apiData.name,
                cnName: apiData.cnName,
                enName: apiData.enName,
                types: apiData.types,
                description: apiData.description,
                atk: apiData.atk,
                def: apiData.def,
                level: apiData.level,
                attribute: apiData.attribute,
                race: apiData.race,
                rawData: apiData.rawData,
                cropParams,
                cardImagePath: cardImage?.path || null,
                portraitPath: portrait?.path || null
            })
        });

        const result = await response.json();

        if (!result.success) {
            throw new Error(result.error);
        }

        showToast('卡牌生成成功！');
        
        // 更新查询结果状态
        if (currentCardData.cardImage) {
            currentCardData.cardImage.hasLocal = true;
        }
        if (currentCardData.portrait) {
            currentCardData.portrait.hasLocal = true;
        }
        displayQueryResult(currentCardData);
        
        // 刷新列表
        loadCards();
    } catch (error) {
        showToast(error.message, 'error');
    } finally {
        document.getElementById('generateCardBtn').disabled = false;
        document.getElementById('generateCardBtn').textContent = '生成卡牌';
    }
}

// ===================== 卡牌列表 =====================

async function loadCards() {
    try {
        const response = await fetch(`${API_BASE}/cards`);
        const result = await response.json();

        if (!result.success) {
            throw new Error(result.error);
        }

        displayCards(result.data);
    } catch (error) {
        showToast('加载卡牌列表失败: ' + error.message, 'error');
    }
}

function displayCards(cards) {
    const container = document.getElementById('cardsContainer');

    if (cards.length === 0) {
        container.innerHTML = '<p class="empty">暂无卡牌数据</p>';
        return;
    }

    container.innerHTML = cards.map(card => `
        <div class="card-item">
            <img src="/VYgo/images/cards/${card.card_id}.png" alt="${card.name}" 
                 onerror="this.src='https://cdn.233.momobako.com/ygopro/pics/${card.card_id}.jpg'">
            <div class="card-item-info">
                <h4>${card.cn_name || card.name}</h4>
                <p>ID: ${card.card_id}</p>
            </div>
            <div class="card-item-actions">
                <button onclick="deleteCard(${card.card_id})" class="delete-btn">删除</button>
            </div>
        </div>
    `).join('');
}

async function deleteCard(cardId) {
    if (!confirm(`确定要删除卡牌 ${cardId} 吗？`)) return;

    try {
        const response = await fetch(`${API_BASE}/cards/${cardId}`, {
            method: 'DELETE'
        });

        const result = await response.json();

        if (!result.success) {
            throw new Error(result.error);
        }

        showToast('卡牌已删除');
        loadCards();
    } catch (error) {
        showToast(error.message, 'error');
    }
}

async function generateFullLocalization() {
    try {
        const btn = document.getElementById('generateLocaleBtn');
        btn.disabled = true;
        btn.textContent = '生成中...';

        const response = await fetch(`${API_BASE}/cards/localization/generate`, {
            method: 'POST'
        });

        const result = await response.json();

        if (!result.success) {
            throw new Error(result.error);
        }

        showToast('本地化文件已生成！');
    } catch (error) {
        showToast(error.message, 'error');
    } finally {
        const btn = document.getElementById('generateLocaleBtn');
        btn.disabled = false;
        btn.textContent = '全量生成本地化';
    }
}

// ===================== 配置管理 =====================

function initConfig() {
    document.getElementById('addDirBtn').addEventListener('click', addExternalDir);
    document.getElementById('saveSettingsBtn').addEventListener('click', saveSettings);
}

async function loadExternalDirs() {
    try {
        const response = await fetch(`${API_BASE}/external-dirs`);
        const result = await response.json();

        if (!result.success) {
            throw new Error(result.error);
        }

        displayExternalDirs(result.data);
    } catch (error) {
        showToast('加载目录列表失败: ' + error.message, 'error');
    }
}

function displayExternalDirs(dirs) {
    const container = document.getElementById('dirsList');

    if (dirs.length === 0) {
        container.innerHTML = '<p class="empty">暂无外部目录</p>';
        return;
    }

    container.innerHTML = dirs.map(dir => `
        <div class="dir-item">
            <div class="dir-item-info">
                <span class="path">${dir.path}</span>
                <span class="meta">类型: ${dir.type} | 优先级: ${dir.priority} | ${dir.description || ''}</span>
            </div>
            <button onclick="deleteDir(${dir.id})" class="delete-btn">删除</button>
        </div>
    `).join('');
}

async function addExternalDir() {
    const path = document.getElementById('dirPath').value.trim();
    const type = document.getElementById('dirType').value;
    const priority = parseInt(document.getElementById('dirPriority').value) || 0;
    const description = document.getElementById('dirDesc').value.trim();

    if (!path) {
        showToast('请输入目录路径', 'warning');
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/external-dirs`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ path, type, priority, description })
        });

        const result = await response.json();

        if (!result.success) {
            throw new Error(result.error);
        }

        showToast('目录添加成功！');
        
        document.getElementById('dirPath').value = '';
        document.getElementById('dirPriority').value = '0';
        document.getElementById('dirDesc').value = '';
        
        loadExternalDirs();
    } catch (error) {
        showToast(error.message, 'error');
    }
}

async function deleteDir(id) {
    if (!confirm('确定要删除这个目录吗？')) return;

    try {
        const response = await fetch(`${API_BASE}/external-dirs/${id}`, {
            method: 'DELETE'
        });

        const result = await response.json();

        if (!result.success) {
            throw new Error(result.error);
        }

        showToast('目录已删除');
        loadExternalDirs();
    } catch (error) {
        showToast(error.message, 'error');
    }
}

async function loadSettings() {
    try {
        const response = await fetch(`${API_BASE}/settings`);
        const result = await response.json();

        if (!result.success) {
            throw new Error(result.error);
        }

        const settings = result.data;
        document.getElementById('modId').value = settings.mod_id || '';
        document.getElementById('localePrefix').value = settings.locale_prefix || '';
    } catch (error) {
        showToast('加载设置失败: ' + error.message, 'error');
    }
}

async function saveSettings() {
    const modId = document.getElementById('modId').value.trim();
    const localePrefix = document.getElementById('localePrefix').value.trim();

    try {
        const response = await fetch(`${API_BASE}/settings`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                mod_id: modId,
                locale_prefix: localePrefix
            })
        });

        const result = await response.json();

        if (!result.success) {
            throw new Error(result.error);
        }

        showToast('设置已保存！');
    } catch (error) {
        showToast(error.message, 'error');
    }
}

// ===================== Toast =====================

function showToast(message, type = 'success') {
    const toast = document.getElementById('toast');
    toast.textContent = message;
    toast.className = `toast ${type}`;
    toast.classList.remove('hidden');

    setTimeout(() => {
        toast.classList.add('hidden');
    }, 3000);
}
