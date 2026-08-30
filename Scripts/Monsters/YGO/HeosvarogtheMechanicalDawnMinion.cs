using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Minion;
using VYgo.Scripts.Actions;
using VYgo.Scripts.Cards.Category.Fusion;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;

namespace VYgo.Scripts.Monsters.YGO;

public class HeosvarogtheMechanicalDawnMinion : BaseMonster {
    public override int CardId => 8963089;

    public override bool BasicAttackAction => false;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options) {
        if (options.Source is not HeosvarogtheMechanicalDawn sourceCard) return;

        await ApplyMonsterAction<HeosvarogtheMechanicalDawnAction>(
            choiceContext,
            Creature,
            sourceCard.NegatingAmount,
            owner.Creature,
            sourceCard,
            true);

        CardModel? polymerization = PileType.Discard.GetPile(owner).Cards
            .OfType<Polymerization>()
            .FirstOrDefault();
        if (polymerization != null) {
            await CardPileCmd.Add(polymerization, PileType.Hand);
        }
    }
}
