using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
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
    
    
    protected IEnumerable<Creature> TrySelectLinkMaterials(CoreCard card) {
        var monsters = Owner.Creature.Pets
            .Where(c => c.Monster is BaseMonster);
        //todo 判断
        return monsters;
    }
}