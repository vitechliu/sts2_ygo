using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Minion;
using VYgo.Scripts.Cards.Category.YgoEvent;

namespace VYgo.Scripts.Monsters.YGO;

public class AleistertheInvokerMinion : BaseMonster {
    public override int CardId => 86120751;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not AleistertheInvoker) return;

        CardModel? invocation = PileType.Draw.GetPile(owner).Cards
            .OfType<Invocation>()
            .FirstOrDefault();
        if (invocation != null) {
            await CardPileCmd.Add(invocation, PileType.Hand);
        }
    }
}
