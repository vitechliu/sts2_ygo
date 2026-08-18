using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MinionLib.Minion;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Playmaker;

namespace VYgo.Scripts.Monsters.YGO;

public class LadyDebugMinion: BaseMonster {
    public override int CardId => 16188701;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not LadyDebug sourceCard) return;

        List<CardModel> choices = CardFactory.GetDistinctForCombat(
                owner,
                ModelDb.AllCards
                    .OfType<BaseMonsterCard>()
                    .Where(IsCyberseMonster),
                sourceCard.ChoiceCount,
                owner.RunState.Rng.CombatCardGeneration)
            .ToList();
        if (choices.Count == 0) return;

        if (sourceCard.IsUpgraded) {
            CardCmd.Upgrade(choices, CardPreviewStyle.HorizontalLayout);
        }

        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            choices,
            owner);
        if (selected != null) {
            await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, owner);
        }
    }

    protected override async Task OnSendToGraveyard(
        PlayerChoiceContext choiceContext,
        Creature creature,
        Player owner
    ) {
        if (SourceCard != null) {
            await CardCmd.Exhaust(choiceContext, SourceCard);
        }
    }

    private static bool IsCyberseMonster(BaseMonsterCard card) {
        return !card.IsExtra && card.YgoGetCore().IsRace(YgoRace.Cyberse);
    }
}
