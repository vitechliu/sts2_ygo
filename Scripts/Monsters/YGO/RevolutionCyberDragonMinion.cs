using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Minion;
using VYgo.Scripts.Actions;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;

namespace VYgo.Scripts.Monsters.YGO;

public class RevolutionCyberDragonMinion : BaseMonster {
    public override int CardId => 66664203;

    public override bool BasicAttackAction => false;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        await ApplyMonsterAction<RevolutionCyberDragonFusionAction>(
            choiceContext,
            Creature,
            1m,
            owner.Creature,
            options.Source,
            true
        );

        if (options.Source is not RevolutionCyberDragon sourceCard) return;

        Creature? target = owner.RunState.Rng.CombatTargets.NextItem(
            Creature.CombatState.HittableEnemies
        );
        if (target == null) return;

        await CreatureCmd.Damage(
            choiceContext,
            target,
            sourceCard.EnterDamage,
            ValueProp.Move,
            Creature,
            sourceCard,
            null
        );
    }
}
