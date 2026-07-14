using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Powers;

namespace VYgo.Patches;

[HarmonyPatch(typeof(PowerModel), "IsVisibleInternal", MethodType.Getter)]
public static class MinionGuardianPowerPatches {
    [HarmonyPrefix]
    public static bool HideMinionGuardianPower(PowerModel __instance, ref bool __result) {
        if (__instance is not MinionGuardianPower) return true;

        __result = false;
        return false;
    }
}
