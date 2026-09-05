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
public class ForbiddenChalice() : BaseSpellCard(0, CardType.Skill, CardRarity.Uncommon, MinionTargetTypes.AnyCreature) {
    public override int CardId => 25789292;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Strength", 4m),
        new PowerVar<VulnerablePower>(2m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<VulnerablePower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await PowerCmd.Apply<ForbiddenChaliceStrengthPower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars["Strength"].BaseValue,
            Owner.Creature,
            this);
        await PowerCmd.Apply<VulnerablePower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars.Vulnerable.BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() {
        DynamicVars["Strength"].UpgradeValueBy(2m);
    }
}
