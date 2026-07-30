using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Commands;
using VYgo.Scripts.Monsters;

namespace VYgo.Scripts.Actions;

public sealed class GroupAttackAction : TargetingAttackAction {
    public override TargetType TargetType => TargetType.AllEnemies;

    protected override async Task OnAct(
        PlayerChoiceContext choiceContext,
        Creature? target) {
        var combatState = Owner.CombatState;
        if (combatState == null) return;

        var animationTarget = GetValidTargets(combatState).FirstOrDefault();
        if (animationTarget == null
            || Owner.Monster is not BaseMonster { SourceCard: { } sourceCard }) {
            return;
        }

        SpendUses();
        await MinionAnimCmd.PlayBumpAttackAsync(Owner, animationTarget);
        await DamageCmd.Attack(StrengthPowerAmount)
            .FromCard(sourceCard, null)
            .TargetingAllOpponents(combatState)
            .WithNoAttackerAnim()
            .Execute(choiceContext);
    }
}
