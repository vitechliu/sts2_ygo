class ValidationError extends Error {}

function parseCardId(value) {
    const text = String(value ?? '').trim();
    if (!/^\d+$/.test(text)) {
        throw new ValidationError('Card ID must be a positive integer');
    }

    const cardId = Number(text);
    if (!Number.isSafeInteger(cardId) || cardId <= 0) {
        throw new ValidationError('Card ID must be a positive safe integer');
    }
    return cardId;
}

function parseDatabaseId(value) {
    const id = Number(value);
    if (!Number.isSafeInteger(id) || id <= 0) {
        throw new ValidationError('ID must be a positive integer');
    }
    return id;
}

function normalizePriority(value) {
    const priority = value === undefined || value === null || value === '' ? 0 : Number(value);
    if (!Number.isSafeInteger(priority)) {
        throw new ValidationError('Priority must be an integer');
    }
    return priority;
}

function requireString(value, fieldName, options = {}) {
    const { allowEmpty = false, maxLength = 4096 } = options;
    if (typeof value !== 'string') {
        throw new ValidationError(`${fieldName} must be a string`);
    }

    const normalized = value.trim();
    if (!allowEmpty && normalized.length === 0) {
        throw new ValidationError(`${fieldName} is required`);
    }
    if (normalized.length > maxLength) {
        throw new ValidationError(`${fieldName} is too long`);
    }
    return normalized;
}

function normalizeCropParams(value) {
    if (value === undefined || value === null) return null;
    if (typeof value !== 'object' || Array.isArray(value)) {
        throw new ValidationError('Crop parameters must be an object');
    }

    const result = {};
    for (const key of ['x', 'y', 'width', 'height', 'sourceWidth', 'sourceHeight']) {
        const number = Number(value[key]);
        if (!Number.isFinite(number)) {
            throw new ValidationError(`Crop parameter ${key} must be a finite number`);
        }
        result[key] = number;
    }

    if (result.x < 0 || result.y < 0
        || result.width <= 0 || result.height <= 0
        || result.sourceWidth <= 0 || result.sourceHeight <= 0) {
        throw new ValidationError('Crop dimensions and source dimensions must be positive');
    }
    return result;
}

module.exports = {
    ValidationError,
    parseCardId,
    parseDatabaseId,
    normalizePriority,
    requireString,
    normalizeCropParams
};
