using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using VYgo.Scripts;
using VYgo.Scripts.Cards;

namespace VYgo.Patches;

[HarmonyPatch]
public class PlayerCardPatches {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Player), nameof(Player.PopulateCombatState))]
    public static bool PopulateCombatStatePatch(Player __instance, Rng rng, CombatState state) {
        if (__instance.Character is not RedhatCharacter) return true;
        Entry.Logger.Info("Patched PopulateCombatState");
        foreach (CardModel mutableCard in __instance.Deck.Cards.ToList()) {
            CardModel card = state.CloneCard(mutableCard);
            card.DeckVersion = mutableCard;
            if (card is BaseMonsterCard mCard && mCard.IsExtra) {
                var pile = Entry.ExtraPile.GetPile(__instance);
                pile.AddInternal(card);
            }
            else {
                __instance.PlayerCombatState.DrawPile.AddInternal(card);
            }
        }
        __instance.PlayerCombatState.DrawPile.RandomizeOrderInternal(__instance, rng, state);
        return false;
    }
}