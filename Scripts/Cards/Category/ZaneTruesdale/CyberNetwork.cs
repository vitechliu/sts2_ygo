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
public class CyberNetwork() : BaseTrapCard(1, CardType.Power, CardRarity.Uncommon, TargetType.None) {
    public override int CardId => 12670770;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar("ExhaustCount", 1),
        new DynamicVar("Turns", 3m),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<CyberNetworkPower>(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        YgoHoverTipConst.SpecialSummon(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        CyberNetworkPower? power = await PowerCmd.Apply<CyberNetworkPower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
        power?.Configure(
            DynamicVars["Turns"].IntValue,
            DynamicVars["ExhaustCount"].IntValue,
            IsUpgraded);
    }
}
