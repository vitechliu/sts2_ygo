using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Utils;

namespace VYgo.Scripts.Cards;

public abstract class BaseExtraCard(
    int baseCost,
    CardType type,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : BaseMonsterCard(baseCost, type, rarity, target, showInCardLibrary) {
    
    //最大随从数量限制
    protected override bool IsPlayable => Owner.MinionCount() < Owner.GetMaxMinionCount();

    public override bool IsExtra => true;
}
