using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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
    private const string AttackCardVar = "AttackCard";
    private const string DefenseCardVar = "DefenseCard";

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LargeCapsule), "CanonicalVars", MethodType.Getter)]
    public static void LargeCapsuleCanonicalVarsPostfix(ref IEnumerable<DynamicVar> __result) {
        __result = __result.Concat([
            new StringVar(AttackCardVar),
            new StringVar(DefenseCardVar),
        ]);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RelicModel), "CanonicalVars", MethodType.Getter)]
    public static void NeowsTalismanCanonicalVarsPostfix(
        RelicModel __instance,
        ref IEnumerable<DynamicVar> __result) {
        // 涅奥的护符沿用基类的变量 getter，只为此遗物补充卡名变量。
        if (__instance is not NeowsTalisman) return;

        __result = __result.Concat([
            new StringVar(AttackCardVar),
            new StringVar(DefenseCardVar),
        ]);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(RelicModel), nameof(RelicModel.DynamicDescription), MethodType.Getter)]
    public static void StarterCardRelicDynamicDescriptionPrefix(RelicModel __instance) {
        UpdateStarterCardNames(__instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(RelicModel), nameof(RelicModel.DynamicEventDescription), MethodType.Getter)]
    public static void StarterCardRelicDynamicEventDescriptionPrefix(RelicModel __instance) {
        UpdateStarterCardNames(__instance);
    }

    private static void UpdateStarterCardNames(RelicModel relic) {
        if (relic is not (LargeCapsule or NeowsTalisman) || !relic.IsMutable) return;

        var owner = relic.Owner;
        if (owner?.Character is not ILargeCapsuleCardProvider provider) return;

        ((StringVar)relic.DynamicVars[AttackCardVar]).StringValue =
            provider.LargeCapsuleAttackCard.Title;
        ((StringVar)relic.DynamicVars[DefenseCardVar]).StringValue =
            provider.LargeCapsuleDefenseCard.Title;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NeowsTalisman), nameof(NeowsTalisman.AfterObtained))]
    public static bool NeowsTalismanAfterObtainedPrefix(NeowsTalisman __instance, ref Task __result) {
        if (__instance.Owner.Character is not ILargeCapsuleCardProvider provider) return true;

        var deck = PileType.Deck.GetPile(__instance.Owner);
        // 复用巨大扭蛋的角色映射，按模型身份寻找牌组中仍可升级的对应牌。
        foreach (var model in new[] { provider.LargeCapsuleAttackCard, provider.LargeCapsuleDefenseCard }) {
            var card = deck.Cards.LastOrDefault(card => card.Id == model.Id && card.IsUpgradable);
            if (card != null) CardCmd.Upgrade(card);
        }

        __result = Task.CompletedTask;
        return false;
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
