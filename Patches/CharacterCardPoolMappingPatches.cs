using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using VYgo.Core.CardPools;
using VYgo.Core.Extensions;
using VYgo.Scripts;

namespace VYgo.Patches;

[HarmonyPatch]
public static class CharacterCardPoolMappingPatches {
    private static readonly CardType[] YgoCharacterCardTypes = [
        CardType.Skill,
        CardType.Skill,
        CardType.Skill,
        CardType.Skill,
        CardType.Power,
    ];

    private static readonly FieldInfo? ColoredCardTypesField =
        AccessTools.Field(typeof(MerchantInventory), "_coloredCardTypes");

    private static readonly FieldInfo? CharacterCardEntriesField =
        AccessTools.Field(typeof(MerchantInventory), "_characterCardEntries");

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
    [HarmonyPatch(typeof(MerchantInventory), "PopulateCharacterCardEntries")]
    public static bool PopulateCharacterCardEntriesPrefix(MerchantInventory __instance) {
        var player = __instance.Player;
        var character = player.Character;
        var hasExtraPools = CharacterCardPoolLinks.HasExtraPools(character);
        var isYgoCharacter = character.GetType().IsGenericTypeOf(typeof(BaseYgoCharacter<,,>));
        if (!hasExtraPools && !isYgoCharacter) return true;

        CardType[] coloredCardTypes;
        if (isYgoCharacter) {
            coloredCardTypes = YgoCharacterCardTypes;
        }
        else if (ColoredCardTypesField?.GetValue(null) is CardType[] originalColoredCardTypes) {
            coloredCardTypes = originalColoredCardTypes;
        }
        else {
            return true;
        }

        if (CharacterCardEntriesField?.GetValue(__instance) is not List<MerchantCardEntry> characterCardEntries) return true;

        var saleIndex = player.PlayerRng.Shops.NextInt(coloredCardTypes.Length);
        var cardPool = hasExtraPools
            ? CharacterCardPoolLinks.GetUnlockedCardsFor(player).ToList()
            : character.CardPool
                .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
                .ToList();

        for (var i = 0; i < coloredCardTypes.Length; i++) {
            var entry = new MerchantCardEntry(player, __instance, cardPool, coloredCardTypes[i]);
            entry.Populate();
            characterCardEntries.Add(entry);

            if (saleIndex == i) {
                entry.SetOnSale();
            }
        }

        return false;
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
