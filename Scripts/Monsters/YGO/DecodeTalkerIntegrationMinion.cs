using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Scripts.Cards;

namespace VYgo.Scripts.Monsters.YGO;

public class DecodeTalkerIntegrationMinion : BaseMonster {
    public override int CardId => 74665150;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants) {
        if (!Creature.IsAlive || Creature.PetOwner is not { } owner || side != owner.Creature.Side) return;
        var pile = Entry.ExtraPile.GetPile(owner);
        if (!pile.Cards.Any(IsCyberseExtraMonster)) return;
        CardModel? selected = (await CardSelectCmd.FromCombatPile(choiceContext, pile, owner,
            new CardSelectorPrefs(SourceCard!.SelectionScreenPrompt, 1), IsCyberseExtraMonster)).FirstOrDefault();
        if (selected != null && selected.Pile == pile) await CardPileCmd.Add(selected, PileType.Discard);
    }

    private static bool IsCyberseExtraMonster(CardModel card) =>
        card is BaseMonsterCard { IsExtra: true } monster && monster.YgoGetCore().IsRace(YgoRace.Cyberse);
}
