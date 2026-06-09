using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Scaffolding.Content;

namespace VYgo.Scripts.Cards.Placeholders;

public abstract class BasePlaceholder(
    CardType type,
    CardRarity rarity)
    : ModCardTemplate(0, type, rarity, TargetType.None) {
    
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://VYgo/images/card_placeholder.png"
    );
}