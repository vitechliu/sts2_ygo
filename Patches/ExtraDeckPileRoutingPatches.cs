using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core.Cards;

namespace VYgo.Patches;

/// <summary>
/// 在牌堆移动发生前统一改写额外卡组怪兽的目标牌堆。
/// </summary>
[HarmonyPatch]
public static class ExtraDeckPileRoutingPatches {
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(
        typeof(CardPileCmd),
        nameof(CardPileCmd.Add),
        [
            typeof(IEnumerable<CardModel>),
            typeof(CardPile),
            typeof(CardPilePosition),
            typeof(AbstractModel),
            typeof(bool),
            typeof(bool),
        ])]
    public static bool RedirectCardPileCmdAdd(
        ref IEnumerable<CardModel> cards,
        ref CardPile newPile,
        CardPilePosition position,
        AbstractModel? clonedBy,
        bool skipVisuals,
        bool isChangingOwners,
        ref Task<IReadOnlyList<CardPileAddResult>> __result) {
        List<CardModel> cardList = cards.ToList();
        cards = cardList;

        if (cardList.Count == 0
            || newPile.Type is not (PileType.Hand or PileType.Draw)) {
            return true;
        }

        CardPile requestedPile = newPile;
        bool hasExtraCard = cardList.Any(card => ExtraDeckPileRouter.ShouldRedirect(card, requestedPile));
        if (!hasExtraCard) {
            return true;
        }

        bool allExtraCards = cardList.All(card => ExtraDeckPileRouter.ShouldRedirect(card, requestedPile));
        if (allExtraCards) {
            newPile = ExtraDeckPileRouter.Resolve(cardList[0], requestedPile);
            return true;
        }

        __result = AddMixedBatch(
            cardList,
            requestedPile,
            position,
            clonedBy,
            skipVisuals,
            isChangingOwners);
        return false;
    }

    /// <summary>
    /// 混合批次按连续目标牌堆分段执行，避免改变输入顺序、随机数消耗和 Hook 顺序。
    /// </summary>
    private static async Task<IReadOnlyList<CardPileAddResult>> AddMixedBatch(
        IReadOnlyList<CardModel> cards,
        CardPile requestedPile,
        CardPilePosition position,
        AbstractModel? clonedBy,
        bool skipVisuals,
        bool isChangingOwners) {
        var owner = cards[0].Owner;
        if (cards.Any(card => card.Owner != owner)) {
            throw new InvalidOperationException("Tried to add cards with different owners to the same pile!");
        }

        List<CardPileAddResult> results = new(cards.Count);
        int startIndex = 0;
        while (startIndex < cards.Count) {
            CardPile targetPile = ExtraDeckPileRouter.Resolve(cards[startIndex], requestedPile);
            int endIndex = startIndex + 1;
            while (endIndex < cards.Count
                   && ReferenceEquals(
                       ExtraDeckPileRouter.Resolve(cards[endIndex], requestedPile),
                       targetPile)) {
                endIndex++;
            }

            List<CardModel> segment = cards
                .Skip(startIndex)
                .Take(endIndex - startIndex)
                .ToList();
            IReadOnlyList<CardPileAddResult> segmentResults = await CardPileCmd.Add(
                segment,
                targetPile,
                position,
                clonedBy,
                skipVisuals,
                isChangingOwners);
            results.AddRange(segmentResults);
            startIndex = endIndex;
        }

        return results;
    }

    /// <summary>
    /// 保护绕过 CardPileCmd 的引擎内部路径，例如开局克隆和战斗中变形。
    /// 正常命令路径已在上方重定向，因此这里仅作为最终不变量保护。
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(CardPile), nameof(CardPile.AddInternal))]
    public static bool RedirectCardPileAddInternal(
        CardPile __instance,
        CardModel card,
        int index,
        bool silent) {
        if (!ExtraDeckPileRouter.ShouldRedirect(card, __instance)) {
            return true;
        }

        CardPile extraPile = ExtraDeckPileRouter.Resolve(card, __instance);
        int redirectedIndex = index < 0
            ? -1
            : Math.Min(index, extraPile.Cards.Count);
        extraPile.AddInternal(card, redirectedIndex, silent);
        return false;
    }
}
