using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace VYgo.Scripts.Cards;

public abstract class BaseTokenCard(
    TargetType target,
    bool showInCardLibrary = true)
    : BaseMonsterCard(0, CardType.Skill, CardRarity.Token, target, showInCardLibrary) {
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    /// <summary>
    /// 衍生物离场时不进入其他区域，而是直接从本场战斗中移除。
    /// 返回 false 说明调用方传入的卡已经不在战斗牌堆中，无法安全执行移除。
    /// </summary>
    internal async Task<bool> DisappearFromCombat() {
        if (Pile == null) return true;
        if (!Pile.IsCombatPile) {
            Entry.Logger.Error(
                $"Token card {GetType().Name} cannot disappear from non-combat pile {Pile.Type}."
            );
            return false;
        }

        await CardPileCmd.RemoveFromCombat(this, skipVisuals: true);
        return Pile == null;
    }
}
