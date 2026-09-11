using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Scripts.Powers;
namespace VYgo.Scripts.Cards.Category.Playmaker;
[RegisterCard(typeof(PlaymakerCardPool))]
public class CynetCodec() : BaseSpellCard(1, CardType.Power, CardRarity.Rare, TargetType.None) {
    public override int CardId => 60018643;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<CynetCodecPower>()];
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        var power = await PowerCmd.Apply<CynetCodecPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        power?.Configure(IsUpgraded);
    }
}
