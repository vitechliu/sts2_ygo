using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;

namespace VYgo.Scripts.Cards;

public abstract class BaseTrapCard(
    int baseCost,
    CardType cardType,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : BaseVYgoCard(baseCost, cardType, rarity, target, showInCardLibrary) {
    protected override YgoType CardYgoType => YgoType.trap;
}