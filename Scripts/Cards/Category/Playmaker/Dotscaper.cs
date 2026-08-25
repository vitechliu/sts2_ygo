using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class Dotscaper() : BaseMonsterCard(0, CardRarity.Uncommon, TargetType.None) {
    public override int CardId => 18789533;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 1;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.SpecialSummon(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
    ];

    public override async Task AfterCardExhausted(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool causedByEthereal
    ) {
        if (card != this
            || CombatState is not { } combatState
            || Owner.MinionCount() >= Owner.GetMaxMinionCount()
            || !this.CanUseEffectOncePerDuelByCard(combatState, Owner, "exhaust")) {
            return;
        }

        await CardCmd.AutoPlay(choiceContext, this, null);
    }
}
