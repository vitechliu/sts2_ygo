using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using VYgo.Core.CardPools;

namespace VYgo.Patches;

[HarmonyPatch]
public static class CharacterCardPoolMappingPatches {
    private static readonly FieldInfo? DiscoveryMockSelectedCardField =
        AccessTools.Field(typeof(Discovery), "_mockSelectedCard");

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyCardRewardCreationOptions))]
    public static void ModifyCardRewardCreationOptionsPostfix(Player player, ref CardCreationOptions __result) {
        if (!CharacterCardPoolLinks.HasExtraPools(player.Character)) return;
        if (__result.Flags.HasFlag(CardCreationFlags.NoCardPoolModifications)) return;
        // if (__result.CustomCardPool != null) return;

        var primaryPool = player.Character.CardPool;
        if (__result.CardPools.All(pool => pool.Id != primaryPool.Id)) return;

        var mappedPools = CharacterCardPoolLinks.GetPoolsFor(player.Character);
        var expandedPools = __result.CardPools
            .SelectMany(pool => pool.Id == primaryPool.Id ? mappedPools : [pool])
            .DistinctBy(static pool => pool.Id)
            .ToList();

        __result = __result.WithCardPools(expandedPools);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Discovery), "OnPlay")]
    public static bool DiscoveryOnPlayPrefix(
        Discovery __instance,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ref Task __result) {
        var owner = __instance.Owner;
        if (!CharacterCardPoolLinks.HasExtraPools(owner.Character)) return true;

        var mockSelectedCard = DiscoveryMockSelectedCardField?.GetValue(__instance) as CardModel;
        __result = PlayDiscovery(__instance, choiceContext, mockSelectedCard);
        return false;
    }

    private static async Task PlayDiscovery(
        Discovery discovery,
        PlayerChoiceContext choiceContext,
        CardModel? mockSelectedCard) {
        var owner = discovery.Owner;
        CardModel? selectedCard;

        if (mockSelectedCard == null) {
            var cards = CardFactory.GetDistinctForCombat(
                owner,
                CharacterCardPoolLinks.GetUnlockedCardsFor(owner),
                3,
                owner.RunState.Rng.CombatCardGeneration).ToList();

            selectedCard = await CardSelectCmd.FromChooseACardScreen(choiceContext, cards, owner, canSkip: true);
        }
        else {
            selectedCard = mockSelectedCard;
        }

        if (selectedCard != null) {
            selectedCard.SetToFreeThisTurn();
            await CardPileCmd.AddGeneratedCardToCombat(selectedCard, PileType.Hand, owner);
        }
    }
}
