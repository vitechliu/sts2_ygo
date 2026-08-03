using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;

namespace VYgo.Scripts.Monsters.YGO;

public class ArmoredCybernMinion: BaseMonster {
    public override int CardId => 67159705;

    public override bool BasicAttackAction => false;

    public override Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options) {
        return PowerCmd.Apply<ArmoredCybernAction>(
            choiceContext,
            Creature,
            1m,
            owner.Creature,
            options.Source,
            true
        );
    }
}
