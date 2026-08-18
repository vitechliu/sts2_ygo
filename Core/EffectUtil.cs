using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using VYgo.Core.History;
using VYgo.Scripts.Cards;

namespace VYgo.Core;

//控制效果发动次数
public static class EffectUtil {
    /// <summary>
    /// 同一卡名的指定效果一场战斗只能发动一次。
    /// </summary>
    public static bool CanUseEffectByCard(
        this IYgoId ygoIdContent, 
        ICombatState combatState, 
        CardPlay cardPlay, string 
        effectSign = "default") {
        return ygoIdContent.CanUseEffectByCard(
            combatState,
            cardPlay.Player,
            effectSign);
    }

    /// <summary>
    /// 同一卡名的指定效果一场战斗只能发动一次。
    /// </summary>
    public static bool CanUseEffectByCard(
        this IYgoId ygoIdContent,
        ICombatState combatState,
        Player player,
        string effectSign = "default"
    ) {
        if (CombatManager.Instance.History.Entries
            .OfType<EffectEntry>()
            .Any(entry => entry.CardId == ygoIdContent.CardId
                && entry.Sign == effectSign
                && entry.Player == player)) {
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
