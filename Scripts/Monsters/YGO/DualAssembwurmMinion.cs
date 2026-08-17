using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;
using VYgo.Scripts.Cards.Category.Playmaker;

namespace VYgo.Scripts.Monsters.YGO;

public class DualAssembwurmMinion : BaseMonster {
    public override int CardId => 7445307;

    public override bool BasicAttackAction => false;

    public override Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not DualAssembwurm sourceCard) return Task.CompletedTask;

        return PowerCmd.Apply<DualAssembwurmAction>(
            choiceContext,
            Creature,
            sourceCard.Damage,
            owner.Creature,
            sourceCard,
            true
        );
    }
}
