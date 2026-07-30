using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;

namespace VYgo.Scripts.Monsters.YGO;

public class CyberdarkEdgeMinion: BaseMonster {
    public override int CardId => 77625948;

    public override bool BasicAttackAction => false;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options) {
        await PowerCmd.Apply<GroupAttackAction>(
            choiceContext,
            Creature,
            1m,
            owner.Creature,
            options.Source,
            true);
    }
}
