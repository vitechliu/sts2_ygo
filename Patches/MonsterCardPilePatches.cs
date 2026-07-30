using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
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

        skipVisuals = true;
    }
}
