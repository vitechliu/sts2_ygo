using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Scaffolding.Content;

namespace VYgo.Scripts.Powers;

public abstract class BaseActionPower : ModPowerTemplate, IModRightClickablePower {

    protected bool _activated;

    public virtual async Task OnRightClick(ModRightClickExecutionContext context) {
        if (!CanExecuteRightClick(context)) return;
        _activated = await OnAction(context);
    }

    public virtual bool CanExecuteRightClick(ModRightClickExecutionContext context) {
        return context.PlayerChoiceContext != null
            && Owner.Player == context.Player
            && Amount > 0
            && !_activated;
    }

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState) {
        _activated = false;
        return base.BeforeSideTurnStart(choiceContext, side, participants, combatState);
    }

    /// <summary>
    /// Executes the action and returns whether its once-per-turn use was spent.
    /// </summary>
    protected virtual Task<bool> OnAction(ModRightClickExecutionContext context) {
        return Task.FromResult(false);
    }
}
