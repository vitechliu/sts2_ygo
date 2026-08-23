using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Core.Summon;

namespace VYgo.Scripts.Cards;

/// <summary>
/// 卫星闪灵主卡组怪兽基类：场上有2星·2阶的怪兽时可以特召（费用变为0）。
/// </summary>
public abstract class BaseSprightMonsterCard(int baseCost, CardRarity rarity)
    : BaseMonsterCard(baseCost, rarity, TargetType.None) {

    protected bool IsSpecialSummonActive => YgoSummonRules.ControlsLevel2OrRank2Monster(Owner);

    protected override bool ShouldGlowGoldInternal => IsSpecialSummonActive;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.SpecialSummon()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        await SummonMonster(
            choiceContext,
            cardPlay,
            new SummonContext(IsSpecialSummon: cardPlay.IsAutoPlay || IsSpecialSummonActive)
        );
    }

    public override bool TryModifyEnergyCostInCombat(
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost) {
        modifiedCost = originalCost;
        if (card != this || !IsSpecialSummonActive) return false;

        modifiedCost = 0m;
        return true;
    }
}
