using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using VYgo.Scripts;
using VYgo.Scripts.Characters;

namespace VYgo.Patches;

[HarmonyPatch]
public static class YgoEventPatches {
    private const string LargeCapsuleAttackCardVar = "AttackCard";
    private const string LargeCapsuleDefenseCardVar = "DefenseCard";

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LargeCapsule), "CanonicalVars", MethodType.Getter)]
    public static void LargeCapsuleCanonicalVarsPostfix(ref IEnumerable<DynamicVar> __result) {
        __result = __result.Concat([
            new StringVar(LargeCapsuleAttackCardVar),
            new StringVar(LargeCapsuleDefenseCardVar),
        ]);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(RelicModel), nameof(RelicModel.DynamicDescription), MethodType.Getter)]
    public static void LargeCapsuleDynamicDescriptionPrefix(RelicModel __instance) {
        UpdateLargeCapsuleCardNames(__instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(RelicModel), nameof(RelicModel.DynamicEventDescription), MethodType.Getter)]
    public static void LargeCapsuleDynamicEventDescriptionPrefix(RelicModel __instance) {
        UpdateLargeCapsuleCardNames(__instance);
    }

    private static void UpdateLargeCapsuleCardNames(RelicModel relic) {
        if (relic is not LargeCapsule largeCapsule || !largeCapsule.IsMutable) return;

        var owner = largeCapsule.Owner;
        if (owner?.Character is not ILargeCapsuleCardProvider provider) return;

        ((StringVar)largeCapsule.DynamicVars[LargeCapsuleAttackCardVar]).StringValue =
            provider.LargeCapsuleAttackCard.Title;
        ((StringVar)largeCapsule.DynamicVars[LargeCapsuleDefenseCardVar]).StringValue =
            provider.LargeCapsuleDefenseCard.Title;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(LargeCapsule), "GetStrikeForCharacter")]
    public static bool LargeCapsuleGetStrikeForCharacterPrefix(
        CharacterModel character,
        ref CardModel __result) {
        if (character is not ILargeCapsuleCardProvider provider) return true;

        __result = provider.LargeCapsuleAttackCard;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(LargeCapsule), "GetDefendForCharacter")]
    public static bool LargeCapsuleGetDefendForCharacterPrefix(
        CharacterModel character,
        ref CardModel __result) {
        if (character is not ILargeCapsuleCardProvider provider) return true;

        __result = provider.LargeCapsuleDefenseCard;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TheFutureOfPotions), nameof(TheFutureOfPotions.IsAllowed))]
    public static bool TheFutureOfPotionsIsAllowedPrefix(IRunState runState, ref bool __result) {
        if (!runState.Players.Any(static player => player.IsYgoCharacter())) return true;

        __result = false;
        return false;
    }
}
