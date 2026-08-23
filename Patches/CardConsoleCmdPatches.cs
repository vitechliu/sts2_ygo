using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using VYgo.Scripts;
using VYgo.Scripts.Cards;

namespace VYgo.Patches;

[HarmonyPatch(typeof(CardConsoleCmd), nameof(CardConsoleCmd.Process))]
public static class CardConsoleCmdPatches {
    [HarmonyPrefix]
    public static bool AddExtraCardToExtraPile(
        Player? issuingPlayer,
        string[] args,
        ref CmdResult __result) {
        if (args.Length == 0
            || issuingPlayer == null
            || !RunManager.Instance.IsInProgress
            || CombatManager.Instance.DebugOnlyGetState() is not { } combatState
            || !TargetsHand(args)) {
            return true;
        }

        string cardName = args[0].ToUpperInvariant();
        CardModel? cardModel = ModelDb.AllCards.FirstOrDefault(card => card.Id.Entry == cardName);
        if (cardModel is not BaseExtraCard) {
            return true;
        }

        CardModel card = combatState.CreateCard(cardModel, issuingPlayer);
        Task task = CardPileCmd.Add(card, Entry.ExtraPile);
        __result = new CmdResult(
            task,
            success: true,
            $"Added extra card '{cardModel.Id.Entry}' to the extra deck");
        return false;
    }

    private static bool TargetsHand(string[] args) {
        if (args.Length < 2) {
            return true;
        }

        return Enum.TryParse(args[1], ignoreCase: true, out PileType pileType)
            && Enum.IsDefined(pileType)
            && pileType == PileType.Hand;
    }
}
