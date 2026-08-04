using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;

namespace VYgo.Scripts.Monsters.YGO;

public class CyberKirinMinion: BaseMonster {
    public override int CardId => 76986005;

    public override bool BasicAttackAction => false;

    public override Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options) {
        return PowerCmd.Apply<CyberKirinAction>(
            choiceContext,
            Creature,
            CyberKirin.IntangibleAmount,
            owner.Creature,
            options.Source,
            true);
    }
}
