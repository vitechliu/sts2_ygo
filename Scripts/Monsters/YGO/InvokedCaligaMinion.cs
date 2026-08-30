using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Monsters.YGO;

public class InvokedCaligaMinion : BaseMonster {
    public override int CardId => 13529466;

    public override async Task AfterAttack(
        PlayerChoiceContext choiceContext,
        AttackCommand command
    ) {
        if (!Creature.IsAlive
            || command.Attacker is not { } attacker
            || attacker.Side == Creature.Side
            || command.TargetSide != Creature.Side) {
            return;
        }

        List<Creature> friendlyCharacters = Creature.CombatState.Players
            .Select(player => player.Creature)
            .Where(creature => creature.IsAlive && creature.Side == Creature.Side)
            .ToList();
        if (friendlyCharacters.Count == 0) return;

        await PowerCmd.Apply<InvokedCaligaBufferPower>(
            choiceContext,
            friendlyCharacters,
            1m,
            Creature,
            SourceCard);
    }
}
