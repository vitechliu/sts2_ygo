using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Scaffolding.Content;

namespace VYgo.Scripts.Cards;

public abstract class BaseSummonCard(
    int baseCost,
    CardType type,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : ModCardTemplate(baseCost, type, rarity, target, showInCardLibrary) {

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://VYgo/images/cards/{GetType().Name}.png"
    );
}