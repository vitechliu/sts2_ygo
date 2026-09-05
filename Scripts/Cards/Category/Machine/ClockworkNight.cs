using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Cards.Category.Machine;

[RegisterCard(typeof(MachineCardPool))]
public class ClockworkNight() : BaseSpellCard(1, CardType.Power, CardRarity.Rare, TargetType.None) {
    public override int CardId => 84797028;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("AttackBonus", 3m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<ClockworkNightPower>(),
        YgoHoverTipConst.PowerAction()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        await PowerCmd.Apply<ClockworkNightPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["AttackBonus"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() {
        DynamicVars["AttackBonus"].UpgradeValueBy(1m);
    }
}
