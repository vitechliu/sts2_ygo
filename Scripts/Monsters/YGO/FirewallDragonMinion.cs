using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Playmaker;

namespace VYgo.Scripts.Monsters.YGO;

public class FirewallDragonMinion: BaseMonster {
    public override int CardId => 5043010;

    public override async Task AfterAttack(PlayerChoiceContext choiceContext) {
        if (SourceCard is not FirewallDragon sourceCard || Creature.PetOwner is not { } owner) {
            return;
        }

        int count = Math.Min(
            sourceCard.RecycleCount,
            CardPile.MaxCardsInHand - PileType.Hand.GetPile(owner).Cards.Count);
        if (count <= 0) return;

        List<CardModel> selected = (await CardSelectCmd.FromCombatPile(
            choiceContext,
            PileType.Discard.GetPile(owner),
            owner,
            new CardSelectorPrefs(sourceCard.SelectionScreenPrompt, count),
            card => card is BaseMonsterCard)).ToList();
        if (selected.Count > 0) {
            await CardPileCmd.Add(selected, PileType.Hand);
        }
    }
}
