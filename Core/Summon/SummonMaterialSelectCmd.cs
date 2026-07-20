using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;

namespace VYgo.Core;

public static class SummonMaterialSelectCmd {
    public static async Task<IReadOnlyList<SummonMaterial>> Select(
        PlayerChoiceContext context,
        Player player,
        CardModel targetCard,
        Func<SummonMaterialSelectionSpec?> buildSpec
    ) {
        SummonMaterialSelectionSpec? initialSpec = buildSpec();
        if (initialSpec?.HasValidCombination != true || CombatManager.Instance.IsEnding) {
            return Array.Empty<SummonMaterial>();
        }

        if (CardSelectCmd.Selector != null) {
            return await SelectAutomatically(CardSelectCmd.Selector, buildSpec, initialSpec);
        }

        if (TestMode.IsOn) {
            return initialSpec.FirstValidCombination;
        }

        uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(player);
        bool choiceBegun = false;
        try {
            await context.SignalPlayerChoiceBegun(player, PlayerChoiceOptions.None);
            choiceBegun = true;

            IReadOnlyList<CardModel> selectedCards;
            if (ShouldSelectLocalCard(player)) {
                if (CardSelectCmd.LocalSelector != null) {
                    IReadOnlyList<SummonMaterial> automaticSelection = await SelectAutomatically(
                        CardSelectCmd.LocalSelector,
                        buildSpec,
                        initialSpec
                    );
                    selectedCards = automaticSelection
                        .Select(material => material.Card)
                        .OfType<CardModel>()
                        .ToList();
                }
                else {
                    NPlayerHand.Instance?.CancelAllCardPlay();
                    NSummonMaterialSelectScreen? screen = NSummonMaterialSelectScreen.Create(
                        targetCard,
                        player,
                        buildSpec
                    );
                    if (screen == null || NOverlayStack.Instance == null) {
                        selectedCards = Array.Empty<CardModel>();
                    }
                    else {
                        NOverlayStack.Instance.Push(screen);
                        selectedCards = (await screen.CardsSelected()).ToList();
                    }
                }

                RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
                    player,
                    choiceId,
                    PlayerChoiceResult.FromMutableCombatCards(selectedCards)
                );
            }
            else {
                selectedCards = (await RunManager.Instance.PlayerChoiceSynchronizer
                        .WaitForRemoteChoice(player, choiceId))
                    .AsCombatCards()
                    .ToList();
            }

            SummonMaterialSelectionSpec? latestSpec = buildSpec();
            if (latestSpec?.HasValidCombination != true) {
                return Array.Empty<SummonMaterial>();
            }

            IReadOnlyList<SummonMaterial> materials = latestSpec.ResolveMaterials(selectedCards);
            return latestSpec.IsValidSelection(materials)
                ? materials
                : Array.Empty<SummonMaterial>();
        }
        catch (TaskCanceledException) {
            return Array.Empty<SummonMaterial>();
        }
        finally {
            if (choiceBegun) {
                await context.SignalPlayerChoiceEnded();
            }
        }
    }

    private static async Task<IReadOnlyList<SummonMaterial>> SelectAutomatically(
        ICardSelector selector,
        Func<SummonMaterialSelectionSpec?> buildSpec,
        SummonMaterialSelectionSpec initialSpec
    ) {
        IReadOnlyList<CardModel> selectedCards = (await selector.GetSelectedCards(
                initialSpec.CandidateCards,
                initialSpec.MinSelect,
                initialSpec.MaxSelect))
            .ToList();

        SummonMaterialSelectionSpec? latestSpec = buildSpec();
        if (latestSpec?.HasValidCombination != true) {
            return Array.Empty<SummonMaterial>();
        }

        IReadOnlyList<SummonMaterial> materials = latestSpec.ResolveMaterials(selectedCards);
        return latestSpec.IsValidSelection(materials)
            ? materials
            : latestSpec.FirstValidCombination;
    }

    private static bool ShouldSelectLocalCard(Player player) {
        return LocalContext.IsMe(player)
            && RunManager.Instance.NetService.Type != NetGameType.Replay;
    }
}
