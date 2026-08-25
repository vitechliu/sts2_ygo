using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace VYgo.Scripts.Actions;

public sealed class TemporaryExtraAttackAction : TargetingAttackAction {
    private bool _grantExtraAttacks = true;

    protected override int MaxUses => 1 + (_grantExtraAttacks ? (int)Amount : 0);

    public override Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants) {
        if (participants.Contains(Owner)) {
            _grantExtraAttacks = false;
        }
        return Task.CompletedTask;
    }
}
