using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;

namespace VYgo.Scripts.Cards;

public abstract class BaseExtraFusionCard(
    int baseCost,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : BaseExtraCard(baseCost, rarity, target, showInCardLibrary) {
    
    protected override YgoType CardYgoType => YgoType.fusion;

    public virtual int FusionMaterialCount => 2;
}
