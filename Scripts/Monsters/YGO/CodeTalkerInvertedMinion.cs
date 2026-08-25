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

public class CodeTalkerInvertedMinion: BaseMonster {
    public override int CardId => 45462149;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options) {
        if (options.Source is not CodeTalkerInverted sourceCard
            || owner.MinionCount() >= owner.GetMaxMinionCount()) {
            return;
        }

        BaseMonsterCard? selected = (await CardSelectCmd.FromHand(
                choiceContext,
                owner,
                new CardSelectorPrefs(sourceCard.SelectionScreenPrompt, 1),
                IsCyberseMonster,
                sourceCard))
            .OfType<BaseMonsterCard>()
            .FirstOrDefault();
        if (selected != null && owner.MinionCount() < owner.GetMaxMinionCount()) {
            await CardCmd.AutoPlay(choiceContext, selected, null);
        }
    }

    private static bool IsCyberseMonster(CardModel card) {
        return card is BaseMonsterCard monster
            && !monster.IsExtra
            && monster.YgoGetCore().IsRace(YgoRace.Cyberse);
    }
}
