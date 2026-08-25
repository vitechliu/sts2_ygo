using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Minion;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Utils;

namespace VYgo.Scripts.Monsters.YGO;

public class TranscodeTalkerMinion: BaseMonster {
    public override int CardId => 46947713;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options) {
        if (options.Source is not TranscodeTalker sourceCard
            || owner.MinionCount() >= owner.GetMaxMinionCount()) {
            return;
        }

        BaseExtraLinkCard? selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                PileType.Discard.GetPile(owner),
                owner,
                new CardSelectorPrefs(sourceCard.SelectionScreenPrompt, 1),
                IsLinkMonster))
            .OfType<BaseExtraLinkCard>()
            .FirstOrDefault();
        if (selected != null && owner.MinionCount() < owner.GetMaxMinionCount()) {
            await CardCmd.AutoPlay(choiceContext, selected, null);
        }
    }

    private static bool IsLinkMonster(CardModel card) {
        return card is BaseExtraLinkCard link
            && link.YgoGetCore().IsRace(YgoRace.Cyberse);
    }
}
