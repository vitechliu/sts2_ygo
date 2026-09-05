using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MinionLib.Targeting;
using VYgo.Core;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(CommonCardPool))]
public class ForbiddenLance() : BaseSpellCard(0, CardType.Skill, CardRarity.Uncommon, MinionTargetTypes.AnyCreature) {
    public override int CardId => 27243130;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("StrengthLoss", 8m),
        new PowerVar<IntangiblePower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<IntangiblePower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await PowerCmd.Apply<ForbiddenLanceStrengthPower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars["StrengthLoss"].BaseValue,
            Owner.Creature,
            this);
        await PowerCmd.Apply<IntangiblePower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars["IntangiblePower"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() {
        DynamicVars["StrengthLoss"].UpgradeValueBy(3m);
    }
}
