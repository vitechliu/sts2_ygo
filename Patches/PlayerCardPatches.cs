using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using VYgo.Core.Extensions;
using VYgo.Scripts;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Characters;

namespace VYgo.Patches;

[HarmonyPatch]
public class PlayerCardPatches {
    public static void MoveExtraCardsToExtraPiles(Player player) {
        var pile = Entry.ExtraPile.GetPile(player);
        foreach (CardModel card in PileType.Draw.GetPile(player).Cards.ToList()) {
            if (card is BaseMonsterCard mCard && mCard.IsExtra) {
                pile.AddInternal(card, silent:true);
            }
        }
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Player), nameof(Player.PopulateCombatState))]
    public static bool PopulateCombatStatePatch(Player __instance, Rng rng, CombatState state) {
        if (!__instance.Character.GetType().IsGenericTypeOf(typeof(BaseYgoCharacter<,,>))) return true;
        var playerCombatState = __instance.PlayerCombatState
            ?? throw new InvalidOperationException("Player combat state was not initialized before population.");
        foreach (CardModel mutableCard in __instance.Deck.Cards.ToList()) {
            CardModel card = state.CloneCard(mutableCard);
            card.DeckVersion = mutableCard;
            if (card is BaseMonsterCard mCard && mCard.IsExtra) {
                var pile = Entry.ExtraPile.GetPile(__instance);
                pile.AddInternal(card);
            }
            else {
                playerCombatState.DrawPile.AddInternal(card);
            }
        }
        playerCombatState.DrawPile.RandomizeOrderInternal(__instance, rng, state);
        return false;
    }
}
