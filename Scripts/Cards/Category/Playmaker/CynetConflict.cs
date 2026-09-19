using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core;
using VYgo.Scripts.Powers;
namespace VYgo.Scripts.Cards.Category.Playmaker;
[RegisterCard(typeof(PlaymakerCardPool))]
public class CynetConflict() : BaseTrapCard(0, CardType.Skill, CardRarity.Token, TargetType.None) {
    public override int CardId => 7403341;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Banish", 10)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [YgoHoverTipConst.SetCard(), YgoHoverTipConst.PowerAction(), HoverTipFactory.FromPower<NegatingPower>()];
    protected override CardLocation GetResultLocationForCardPlay() => new(Owner, Entry.SetTrapPile, CardPilePosition.Bottom);
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        var power = await PowerCmd.Apply<CynetConflictPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        if (power != null) power.DynamicVars["Banish"].BaseValue = DynamicVars["Banish"].BaseValue;
    }
    protected override void OnUpgrade() => DynamicVars["Banish"].UpgradeValueBy(5);
}
