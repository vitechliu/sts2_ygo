using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;
using VYgo.Scripts.Cards.Category.Playmaker;

namespace VYgo.Scripts.Monsters.YGO;

public class FlickClownMinion: BaseMonster {
    public override int CardId => 209710;

    public override bool BasicAttackAction => false;

    public override Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not FlickClown sourceCard) return Task.CompletedTask;

        return PowerCmd.Apply<FlickClownAction>(
            choiceContext,
            Creature,
            sourceCard.Draw,
            owner.Creature,
            sourceCard,
            true);
    }
}
