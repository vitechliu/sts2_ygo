using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Targeting;
using VYgo.Core.Potions;

namespace VYgo.Patches;

/// <summary>
/// 补齐 MinionLib 自定义目标类型在药水权威结算阶段的合法性校验。
/// </summary>
[HarmonyPatch(typeof(PotionModel), nameof(PotionModel.IsValidTarget))]
public static class PotionTargetModifierPatches {
    [HarmonyPrefix]
    public static bool IsValidTargetPrefix(
        PotionModel __instance,
        Creature? target,
        ref bool __result) {
        if (!PotionTargetModifier.TryGetModifiedTarget(__instance, out var targetType)
            || !CustomTargetTypeManager.TryGetCustomTargetType(
                targetType,
                out var customTargetType,
                includeBuiltin: false)) {
            return true;
        }

        __result = customTargetType.IsSingleTarget
            ? target != null && customTargetType.IsValidTarget(__instance, target)
            : target == null || customTargetType.IsValidTarget(__instance, target);
        return false;
    }
}
