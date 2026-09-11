using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Ui.Toast;
using VYgo.Core.History;
using VYgo.Scripts.Cards;

namespace VYgo.Core;

//控制效果发动次数
public static class EffectUtil {
    public static void ToastOncePerDuel(BaseVYgoCard model) {
        var body = new LocString("combat_messages", "USE_EFFECT_ERROR_ONCE_PER_DUEL.body");
        body.Add("card", model.Title);
        RitsuToastService.ShowWarning(
            body.GetFormattedText(),
            new LocString("combat_messages", "USE_EFFECT_ERROR.title").GetFormattedText()
        );
    }

    // 只查询历史，不占用发动次数，供右键发动预检复用。
    public static bool HasUsedEffectOncePerDuelByCard(
        this IYgoId ygoIdContent,
        Player player,
        string effectSign = "default"
    ) => CombatManager.Instance.History.Entries
        .OfType<EffectEntry>()
        .Any(entry => entry.CardId == ygoIdContent.CardId
            && entry.Sign == effectSign
            && entry.Player == player);

    /// <summary>
    /// 同一卡名的指定效果一场战斗只能发动一次。
    /// </summary>
    public static bool CanUseEffectOncePerDuelByCard(
        this IYgoId ygoIdContent,
        ICombatState combatState,
        Player player,
        string effectSign = "default"
    ) {
        if (ygoIdContent.HasUsedEffectOncePerDuelByCard(player, effectSign)) {
            return false;
        }

        CombatManager.Instance.History.RecordUseEffect(
            ygoIdContent,
            effectSign,
            combatState,
            player);
        return true;
    }
}
