using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;
using VYgo.Scripts;

namespace VYgo.Patches;

[HarmonyPatch]
public static class YgoEventPatches {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TheFutureOfPotions), nameof(TheFutureOfPotions.IsAllowed))]
    public static bool TheFutureOfPotionsIsAllowedPrefix(IRunState runState, ref bool __result) {
        if (!runState.Players.Any(static player => player.IsYgoCharacter())) return true;

        __result = false;
        return false;
    }
}
