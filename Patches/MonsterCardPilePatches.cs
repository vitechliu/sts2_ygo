using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using VYgo.Scripts;
using VYgo.Scripts.Cards;

namespace VYgo.Patches;

[HarmonyPatch(
    typeof(CardPileCmd),
    nameof(CardPileCmd.Add),
    [typeof(CardModel), typeof(PileType), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool)])]
public static class MonsterCardPilePatches {
    [HarmonyPrefix]
    public static void SkipMonsterPileFlyVfx(
        CardModel card,
        PileType newPileType,
        ref bool skipVisuals) {
        if (skipVisuals
            || card is not BaseMonsterCard
            || card.Pile?.Type != PileType.Play
            || newPileType != Entry.MonsterPile) {
            return;
        }

        // 跳过牌堆动画时，原版不会回收出牌区中的卡牌节点，需要在这里显式清理。
        NCard.FindOnTable(card, PileType.Play)?.QueueFreeSafely();
        skipVisuals = true;
    }
}
