function toExportCardData(card, archetypeCodes = []) {
    const { raw_data, created_at, updated_at, archetypes, archetype_warning, ...rest } = card;
    return {
        ...rest,
        archetypes: [...archetypeCodes]
    };
}

function mergeExportedCard(exportedCards, nextCard) {
    const cards = Array.isArray(exportedCards) ? [...exportedCards] : [];
    const existingIndex = cards.findIndex(item => Number(item.card_id) === Number(nextCard.card_id));

    if (existingIndex >= 0) {
        cards[existingIndex] = nextCard;
    } else {
        cards.unshift(nextCard);
    }

    return { cards, updated: existingIndex >= 0 };
}

module.exports = {
    toExportCardData,
    mergeExportedCard
};

