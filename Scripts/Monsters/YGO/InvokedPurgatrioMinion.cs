using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;
using VYgo.Scripts.Cards.Category.YgoEvent;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Monsters.YGO;

public class InvokedPurgatrioMinion : BaseMonster {
    public override int CardId => 12307878;
    public override bool BasicAttackAction => false;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not InvokedPurgatrio sourceCard) return;

        await ApplyMonsterAction<PenetratingAttackAction>(
            choiceContext,
            Creature,
            1m,
            owner.Creature,
            sourceCard,
            true);

        int enemyCount = Creature.CombatState.Creatures.Count(creature =>
            creature.IsAlive && creature.Side != Creature.Side);
        if (enemyCount > 0) {
            await PowerCmd.Apply<AttackPower>(
                choiceContext,
                Creature,
                sourceCard.EnemyBoostAttack * enemyCount,
                owner.Creature,
                sourceCard);
        }
    }
}
