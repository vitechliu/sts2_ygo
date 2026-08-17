using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Utils;

namespace VYgo.Scripts.Monsters.YGO;

public class StackReviverMinion : BaseMonster {
    public override int CardId => 9523599;

    public override async Task OnUsedAsLinkMaterial(
        PlayerChoiceContext choiceContext,
        Player owner,
        IReadOnlyList<SummonMaterial> materials
    ) {
        if (SourceCard is not StackReviver sourceCard
            || sourceCard.Pile?.Type != PileType.Discard
            || owner.MinionCount() >= MinionUtil.MaxMinionCount) {
            return;
        }

        List<CardModel> otherMaterialCards = materials
            .Select(material => material.Card)
            .OfType<CardModel>()
            .Where(card => card != sourceCard && card is BaseMonsterCard)
            .ToList();
        if (otherMaterialCards.Count == 0) return;

        if ((await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(sourceCard.SelectionScreenPrompt, 1),
                context: choiceContext,
                pile: PileType.Discard.GetPile(owner),
                player: owner,
                filter: otherMaterialCards.Contains
            )).FirstOrDefault() is not { } selectedCard) {
            return;
        }
        if (owner.MinionCount() >= MinionUtil.MaxMinionCount) return;

        await CardCmd.AutoPlay(choiceContext, selectedCard, null);
    }
}
