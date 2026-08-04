using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberneticHorizon() : BaseSpellCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.None) {
    public override int CardId => 63031396;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        await ChooseAndAddGeneratedCard(
            choiceContext,
            ModelDb.AllCards
                .OfType<BaseExtraFusionCard>()
                .Where(IsMachineFusionMonster),
            Entry.ExtraPile);

        await ChooseAndAddGeneratedCard(
            choiceContext,
            ModelDb.AllCards
                .OfType<BaseMonsterCard>()
                .Where(IsCyberMonster),
            PileType.Hand);
    }

    private async Task ChooseAndAddGeneratedCard(
        PlayerChoiceContext choiceContext,
        IEnumerable<CardModel> candidates,
        PileType destination) {
        List<CardModel> choices = CardFactory.GetDistinctForCombat(
                Owner,
                candidates,
                3,
                Owner.RunState.Rng.CombatCardGeneration)
            .ToList();
        if (choices.Count == 0) return;

        if (IsUpgraded) {
            CardCmd.Upgrade(choices, CardPreviewStyle.HorizontalLayout);
        }

        CardModel? selectedCard = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            choices,
            Owner);
        if (selectedCard != null) {
            await CardPileCmd.AddGeneratedCardToCombat(selectedCard, destination, Owner);
        }
    }

    private static bool IsMachineFusionMonster(BaseExtraFusionCard card) {
        return card.YgoGetCore().IsRace(YgoRace.Machine);
    }

    private static bool IsCyberMonster(BaseMonsterCard card) {
        return !card.IsExtra && card.ContainArchetype(YgoArchetypes.Cyber);
    }
}
