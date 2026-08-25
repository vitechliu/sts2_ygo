using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using VYgo.Core;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class CynetRegression() : BaseTrapCard(0, CardType.Power, CardRarity.Common, TargetType.None) {
    public override int CardId => 19943114;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(10m, ValueProp.Move),
        new CardsVar(1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<CynetRegressionPower>(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        YgoHoverTipConst.SetCard(),
        YgoHoverTipConst.SpecialSummon()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        CynetRegressionPower? power = await PowerCmd.Apply<CynetRegressionPower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
        power?.Configure(
            this,
            DynamicVars.Damage.BaseValue,
            DynamicVars.Cards.IntValue);
    }

    protected override void OnUpgrade() {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
