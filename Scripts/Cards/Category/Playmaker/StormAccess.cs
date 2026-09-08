using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class StormAccess() : ModCardTemplate(0, CardType.Skill, CardRarity.Token, TargetType.Self) {
    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Retain,
        CardKeyword.Exhaust
    ];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Choices", 3)];
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://VYgo/images/cards/StormAccess.png");

    public override CardPoolModel VisualCardPool => ModelDb.CardPool<ColorlessCardPool>();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        // 沿用调试瓢虫女郎的主卡组电子界族候选范围。
        List<CardModel> choices = CardFactory.GetDistinctForCombat(
            Owner,
            ModelDb.AllCards.OfType<BaseMonsterCard>()
                .Where(card => !card.IsExtra && card.YgoGetCore().IsRace(YgoRace.Cyberse)),
            (int)DynamicVars["Choices"].BaseValue,
            Owner.RunState.Rng.CombatCardGeneration).ToList();
        if (choices.Count == 0) return;

        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, choices, Owner);
        if (selected == null) return;
        selected.SetToFreeThisTurn();
        await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, Owner);
    }
}
