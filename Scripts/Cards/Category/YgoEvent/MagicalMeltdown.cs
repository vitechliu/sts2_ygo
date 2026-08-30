using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.YgoEvent;

[RegisterCard(typeof(YgoEventCardPool))]
public class MagicalMeltdown() : BaseSpellCard(0, CardType.Skill, CardRarity.Event, TargetType.None) {
    public override int CardId => 47679935;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        CardModel? aleister = PileType.Draw.GetPile(Owner).Cards
            .OfType<AleistertheInvoker>()
            .FirstOrDefault();
        if (aleister != null) {
            await CardPileCmd.Add(aleister, PileType.Hand);
        }

        if (!IsUpgraded) return;

        List<CardModel> choices = CardFactory.GetDistinctForCombat(
                Owner,
                ModelDb.AllCards
                    .OfType<BaseExtraFusionCard>()
                    .Where(InvokedFusionUtil.IsInvokedFusionMonster),
                3,
                Owner.RunState.Rng.CombatCardGeneration)
            .ToList();
        if (choices.Count == 0) return;

        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            choices,
            Owner);
        if (selected != null) {
            await CardPileCmd.AddGeneratedCardToCombat(selected, Entry.ExtraPile, Owner);
        }
    }
}
