using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;

namespace VYgo.Scripts.Monsters.YGO;

public class CyberDragonSiegerMinion: BaseMonster {
    public override int CardId => 46724542;

    public override bool BasicAttackAction => false;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not CyberDragonSieger sourceCard) return;

        await PowerCmd.Apply<CyberDragonSiegerAction>(
            choiceContext,
            Creature,
            sourceCard.BoostAttack,
            owner.Creature,
            sourceCard,
            true
        );
    }
}
