using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;
using VYgo.Utils;

namespace VYgo.Scripts.Cards;

public abstract class BaseExtraLinkCard(
    int baseCost,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : BaseExtraCard(baseCost, rarity, target, showInCardLibrary) {
    
    protected override YgoType CardYgoType => YgoType.link;
}