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
public class BornfromDraconis() : BaseTrapCard(1, CardType.Power, CardRarity.Uncommon, TargetType.None) {
    public override int CardId => 96699830;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar("SummonCount", 1),
        new DynamicVar("BoostAttack", 5m),
        new DynamicVar("BoostLife", 5m),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<BornfromDraconisPower>(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        YgoHoverTipConst.SetCard(),
        YgoHoverTipConst.PowerAction(),
        YgoHoverTipConst.SpecialSummon(),
        YgoHoverTipConst.Enhance(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        BornfromDraconisPower? power = await PowerCmd.Apply<BornfromDraconisPower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
        power?.Configure(
            DynamicVars["SummonCount"].IntValue,
            DynamicVars["BoostAttack"].IntValue,
            DynamicVars["BoostLife"].IntValue);
    }

    protected override void OnUpgrade() {
        EnergyCost.UpgradeBy(-1);
    }
}
