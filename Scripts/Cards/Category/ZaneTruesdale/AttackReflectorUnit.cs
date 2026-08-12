using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class AttackReflectorUnit() : BaseTrapCard(1, CardType.Power, CardRarity.Common, TargetType.None) {
    public override int CardId => 91989718;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("SummonCount", 1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<AttackReflectorUnitPower>(),
        HoverTipFactory.FromCard<CyberBarrierDragon>(),
        YgoHoverTipConst.SetCard(),
        YgoHoverTipConst.PowerAction(),
        YgoHoverTipConst.SpecialSummon(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        await PowerCmd.Apply<AttackReflectorUnitPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["SummonCount"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() {
        DynamicVars["SummonCount"].UpgradeValueBy(1m);
    }
}
