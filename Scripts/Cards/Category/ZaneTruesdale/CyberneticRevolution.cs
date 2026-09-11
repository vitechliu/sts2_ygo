using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberneticRevolution() : BaseTrapCard(2, CardType.Skill, CardRarity.Common, TargetType.None) {
    public override int CardId => 18597560;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<CyberneticRevolutionPower>(), YgoHoverTipConst.SetCard(),
        YgoHoverTipConst.PowerAction(), YgoHoverTipConst.SpecialSummon()
    ];
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        await PowerCmd.Apply<CyberneticRevolutionPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
