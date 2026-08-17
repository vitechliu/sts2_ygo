using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core.History;
using VYgo.Scripts.Cards;

namespace VYgo.Core;

//控制效果发动次数
public static class EffectUtil {
    /// <summary>
    /// 卡名一回合一次
    /// </summary>
    public static bool CanUseEffectByCard(
        this IYgoId ygoIdContent, 
        ICombatState combatState, 
        CardPlay cardPlay, string 
        effectSign = "default") {
        if (CombatManager.Instance.History.Entries
                .OfType<EffectEntry>()
                .Count(entry => entry.CardId == ygoIdContent.CardId) > 0) return false;
        CombatManager.Instance.History.RecordUseEffect(ygoIdContent, effectSign, combatState, cardPlay.Player);
        return true;
    }
}