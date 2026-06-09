using MegaCrit.Sts2.Core.Entities.Cards;

namespace VYgo.Scripts.Cards;

public abstract class BaseMonsterCard(
    int baseCost,
    CardType type,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : BaseVYgoCard(baseCost, type, rarity, target, showInCardLibrary) {
    
}