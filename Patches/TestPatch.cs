using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using VYgo.Scripts;

namespace VYgo.Patches;

[HarmonyPatch]
public static class TestPatch {
    // [HarmonyPostfix]
    // [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.RemoveFromCombat), [typeof(CardModel), typeof(bool)])]
    // public static void RemovePatchTest(CardModel card) {
    //     Entry.Logger.Info("RemoveFromCombat Triggered:" + card.Title);
    // }
    //
    // [HarmonyPostfix]
    // [HarmonyPatch(typeof(CyberDragon), "GetResultPileTypeForCardPlay")]
    // public static void testPile1(PileType __result) {
    //     Entry.Logger.Info("GetResultPileTypeForCardPlay CyberDragon Result:" + __result);
    // }
    //
    // [HarmonyPostfix]
    // [HarmonyPatch(typeof(Hook), "ModifyCardPlayResultPileTypeAndPosition")]
    // public static void testPile1Hook((PileType, CardPilePosition) __result) {
    //     Entry.Logger.Info("ModifyCardPlayResultPileTypeAndPosition Result:" + __result.Item1);
    // }
}
