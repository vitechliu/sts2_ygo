using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;

namespace VYgo.Scripts.Monsters.YGO;

public class CyberLarvaMinion: BaseMonster {
    public override int CardId => 35050257;

    protected override async Task OnSendToGraveyard(
        PlayerChoiceContext choiceContext,
        Creature creature,
        Player owner) {
        List<CardModel> larvae = PileType.Draw.GetPile(owner)
            .Cards
            .OfType<CyberLarva>()
            .Cast<CardModel>()
            .ToList();
        if (larvae.Count == 0) return;

        if (_upgraded) {
            await CardPileCmd.Add(larvae, PileType.Hand);
            return;
        }

        CardModel? larva = owner.RunState.Rng.CombatCardSelection.NextItem(larvae);
        if (larva != null) {
            await CardPileCmd.Add(larva, PileType.Hand);
        }
    }
}
