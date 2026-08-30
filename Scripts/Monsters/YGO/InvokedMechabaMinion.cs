using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Cards.Category.YgoEvent;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Monsters.YGO;

public class InvokedMechabaMinion : BaseMonster {
    public override int CardId => 75286621;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not InvokedMechaba sourceCard) return;

        await PowerCmd.Apply<NegatingPower>(
            choiceContext,
            Creature,
            sourceCard.Negating,
            owner.Creature,
            sourceCard);
    }
}
