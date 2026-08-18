using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;

namespace VYgo.Scripts.Monsters.YGO;

public class RAMClouderMinion: BaseMonster {
    public override int CardId => 9190563;

    public override bool BasicAttackAction => false;

    public override Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        return PowerCmd.Apply<RAMClouderAction>(
            choiceContext,
            Creature,
            1m,
            owner.Creature,
            options.Source,
            true);
    }
}
