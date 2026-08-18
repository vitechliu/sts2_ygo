using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Playmaker;

namespace VYgo.Scripts.Monsters.YGO;

public class FlameBufferloMinion: BaseMonster {
    public override int CardId => 80794697;

    protected override async Task OnSendToGraveyard(
        PlayerChoiceContext choiceContext,
        Creature creature,
        Player owner
    ) {
        if (SourceCard is not FlameBufferlo sourceCard) return;

        CardModel? selected = (await CardSelectCmd.FromHandForDiscard(
                choiceContext,
                owner,
                new CardSelectorPrefs(sourceCard.SelectionScreenPrompt, 1),
                IsCyberseMonster,
                sourceCard))
            .FirstOrDefault();
        if (selected == null) return;

        await CardCmd.Discard(choiceContext, selected);
        await CardPileCmd.Draw(choiceContext, sourceCard.Draw, owner);
    }

    private static bool IsCyberseMonster(CardModel card) {
        return card is BaseMonsterCard monster
            && !monster.IsExtra
            && monster.YgoGetCore().IsRace(YgoRace.Cyberse);
    }
}
