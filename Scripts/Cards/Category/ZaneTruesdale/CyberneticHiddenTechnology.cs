using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberneticHiddenTechnology()
    : BaseTrapCard(1, CardType.Power, CardRarity.Rare, TargetType.None) {
    public override int CardId => 92773018;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<CyberneticHiddenTechnologyPower>(10m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<CyberneticHiddenTechnologyPower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        await PowerCmd.Apply<CyberneticHiddenTechnologyPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["CyberneticHiddenTechnologyPower"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() {
        DynamicVars["CyberneticHiddenTechnologyPower"].UpgradeValueBy(3m);
    }
}
