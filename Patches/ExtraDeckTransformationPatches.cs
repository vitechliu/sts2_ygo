using HarmonyLib;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using VYgo.Scripts.Cards;

namespace VYgo.Patches;

/// <summary>
/// 只从战斗内的随机变化候选中排除额外卡组怪兽。
/// 不修改 CanBeGeneratedInCombat，避免影响刻意发现或生成额外卡组怪兽的卡牌效果。
/// </summary>
[HarmonyPatch]
public static class ExtraDeckTransformationPatches {
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(
        typeof(CardFactory),
        nameof(CardFactory.GetDefaultTransformationOptions),
        [typeof(CardModel), typeof(bool)])]
    public static void FilterDefaultTransformationOptions(
        bool isInCombat,
        ref IEnumerable<CardModel> __result) {
        if (!isInCombat) return;

        __result = ExcludeExtraDeckCards(__result);
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(
        typeof(CardFactory),
        nameof(CardFactory.CreateRandomCardForTransform),
        [typeof(CardModel), typeof(IEnumerable<CardModel>), typeof(bool), typeof(Rng)])]
    public static void FilterProvidedTransformationOptions(
        ref IEnumerable<CardModel> options,
        bool isInCombat) {
        if (!isInCombat) return;

        options = ExcludeExtraDeckCards(options);
    }

    private static IEnumerable<CardModel> ExcludeExtraDeckCards(IEnumerable<CardModel> options) {
        return options.Where(static card => card is not BaseExtraCard);
    }
}
