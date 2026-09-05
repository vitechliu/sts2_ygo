using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Cards.Category.Fusion;

[RegisterCard(typeof(FusionCardPool))]
public class FusionGate() : BaseSpellCard(2, CardType.Power, CardRarity.Rare, TargetType.None) {
    public override int CardId => 33550694;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<FusionGatePower>(),
        YgoHoverTipConst.PowerAction(),
        YgoHoverTipConst.FusionSummon()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        await PowerCmd.Apply<FusionGatePower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() {
        EnergyCost.UpgradeBy(-1);
    }
}
