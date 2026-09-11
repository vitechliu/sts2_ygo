using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using VYgo.Core;
namespace VYgo.Scripts.Cards.Category.Playmaker;
[RegisterCard(typeof(PlaymakerCardPool))]
public class CynetMining() : BaseSpellCard(0, CardType.Skill, CardRarity.Common, TargetType.None) {
    public override int CardId => 57160136;
    protected override bool IsPlayable => PileType.Hand.GetPile(Owner).Cards.Any(card => card != this);
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        var discarded = (await CardSelectCmd.FromHandForDiscard(choiceContext, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1), null, this)).FirstOrDefault();
        if (discarded == null) return;
        await CardCmd.Discard(choiceContext, discarded);
        var choices = CardFactory.GetDistinctForCombat(Owner, ModelDb.AllCards.OfType<BaseMonsterCard>()
            .Where(card => !card.IsExtra && card.Rarity != CardRarity.Token && card.Level is > 0 and <= 4
                && card.YgoGetCore().IsRace(YgoRace.Cyberse)), 3, Owner.RunState.Rng.CombatCardGeneration).ToList();
        if (choices.Count == 0) return;
        if (IsUpgraded) CardCmd.Upgrade(choices, CardPreviewStyle.HorizontalLayout);
        var selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, choices, Owner);
        if (selected != null) await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, Owner);
    }
}
