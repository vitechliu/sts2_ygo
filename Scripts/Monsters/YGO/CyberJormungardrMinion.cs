using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;

namespace VYgo.Scripts.Monsters.YGO;

public class CyberJormungardrMinion : BaseMonster {
    public override int CardId => 19715246;

    public override bool BasicAttackAction => false;

    public override Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        return PowerCmd.Apply<CyberJormungardrAction>(
            choiceContext,
            Creature,
            1m,
            owner.Creature,
            options.Source,
            true
        );
    }
}
