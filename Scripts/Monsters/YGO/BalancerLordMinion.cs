using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;
using VYgo.Scripts.Cards.Category.Playmaker;

namespace VYgo.Scripts.Monsters.YGO;

public class BalancerLordMinion : BaseMonster {
    public override int CardId => 8567955;

    public override bool BasicAttackAction => false;

    public override Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not BalancerLord sourceCard) return Task.CompletedTask;

        return ApplyMonsterAction<BalancerLordAction>(
            choiceContext,
            Creature,
            1,
            owner.Creature,
            sourceCard,
            true
        );
    }
}
