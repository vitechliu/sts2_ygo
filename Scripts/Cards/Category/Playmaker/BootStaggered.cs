using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Core.Hooks;
using VYgo.Core.Summon;
using VYgo.Scripts.Pools;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class BootStaggered() : BaseMonsterCard(2, CardRarity.Uncommon, TargetType.None), IMonsterSummonHookListener {
    public override int CardId => 70950698;

    public override int BaseAttackVar => 8;
    public override int BaseLifeVar => 2;

    private bool _specialSummonFromHand;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.SummonNormal(),
        YgoHoverTipConst.SpecialSummon(),
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay
    ) {
        await SummonMonster(
            choiceContext,
            cardPlay,
            new SummonContext(IsSpecialSummon: _specialSummonFromHand)
        );
    }

    public async Task AfterMonsterSummon(
        PlayerChoiceContext choiceContext,
        BaseMonsterCard card,
        CardPlay cardPlay,
        Creature summonedCreature,
        SummonContext summonContext
    ) {
        if (summonContext.IsSpecialSummon
            || card.Owner != Owner
            || Pile?.Type != PileType.Hand
            || Owner.MinionCount() >= MinionUtil.MaxMinionCount) {
            return;
        }

        _specialSummonFromHand = true;
        try {
            await CardCmd.AutoPlay(choiceContext, this, null);
        }
        finally {
            _specialSummonFromHand = false;
        }
    }
}
