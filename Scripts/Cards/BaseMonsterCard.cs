using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Utils;

namespace VYgo.Scripts.Cards;

public abstract class BaseMonsterCard(
    int baseCost,
    CardType type,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : BaseVYgoCard(baseCost, type, rarity, target, showInCardLibrary) {
    
    //最大随从数量限制
    protected override bool IsPlayable => Owner.MinionCount() < MinionUtil.MAX_MINION_COUNT;

    public virtual bool IsExtra => false;


    public int Life => DynamicVars["Life"].IntValue;
    public int Attack => DynamicVars["Attack"].IntValue;
}