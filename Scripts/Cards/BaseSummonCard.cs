using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core.Cards;
using VYgo.Scripts.Monsters;

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
    
    
    protected IEnumerable<BaseMonster> TrySelectLinkMaterials(CoreCard card) {
        var monsters = Owner.Creature.Pets
            .Select(c => c.Monster)
            .OfType<BaseMonster>();
        //todo 判断
        return monsters;
    }
}