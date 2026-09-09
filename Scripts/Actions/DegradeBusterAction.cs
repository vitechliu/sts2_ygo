using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using VYgo.Core;
using VYgo.Scripts.Monsters;
using VYgo.Scripts.Cards.Category.Playmaker;
namespace VYgo.Scripts.Actions;
public class DegradeBusterAction : BasePerTurnMonsterAction {
    protected override bool IsVisibleInternal => true;
    public override TargetType TargetType => TargetType.AnyEnemy;
    protected override string? IntentIconPath => "res://VYgo/images/powers/ygo.png";
    public override string? CustomIconPath => IntentIconPath;
    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target) {
        if (target == null || Owner.Monster is not BaseMonster { SourceCard: DegradeBuster card }) return;
        SpendUses();
        await BanishCmd.Banish(target, card.DynamicVars["Banish"].IntValue);
    }
}
