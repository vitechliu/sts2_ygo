using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Utils;

namespace VYgo.Scripts.Cards;

public abstract class BaseExtraCard(
    int baseCost,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : BaseMonsterCard(baseCost, rarity, target, showInCardLibrary) {
    
    //最大随从数量限制
    protected override bool IsPlayable => Owner.MinionCount() < MinionUtil.MaxMinionCount;

    public override bool IsExtra => true;
}
