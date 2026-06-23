using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;
using VYgo.Utils;

namespace VYgo.Scripts.Cards;

public abstract class BaseMonsterCard(
    int baseCost,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : BaseVYgoCard(baseCost, CardType.Skill, rarity, target, showInCardLibrary) {


    protected List<IHoverTip> BaseSummonHoverTips => [YgoHoverTipConst.Summon(this)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => BaseSummonHoverTips;
    
    //最大随从数量限制
    protected override bool IsPlayable => Owner.MinionCount() < MinionUtil.MAX_MINION_COUNT;

    public virtual bool IsExtra => false;
    
    //怪兽卡打出后和能力卡一样消失，只有怪兽死亡后才会移入弃牌堆
    protected override PileType GetResultPileTypeForCardPlay()
    {
        return PileType.None;
    }


    public int Life => DynamicVars["Life"].IntValue;
    public int Attack => DynamicVars["Attack"].IntValue;
}