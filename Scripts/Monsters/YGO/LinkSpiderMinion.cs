using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;

namespace VYgo.Scripts.Monsters.YGO;

public class LinkSpiderMinion: BaseMonster {
    public override int CardId => 98978921;

    public override bool BasicAttackAction => false;

    public override Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        return ApplyMonsterAction<LinkSpiderAction>(
            choiceContext,
            Creature,
            1m,
            owner.Creature,
            options.Source,
            true);
    }
}
