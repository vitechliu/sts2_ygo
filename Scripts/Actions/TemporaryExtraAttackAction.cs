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
        // 玩家回合结束的参与者只有玩家本体，随从需要通过持有者判断。
        if (side == Owner.Side && (participants.Contains(Owner)
            || Owner.PetOwner is { } player && participants.Contains(player.Creature))) {
            _grantExtraAttacks = false;
        }
        return Task.CompletedTask;
    }
}
