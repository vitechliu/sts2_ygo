using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Ui.Toast;

namespace VYgo.Scripts.Cards;

public abstract class BaseRightClickableMonsterCard(
    int baseCost,
    CardType type,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : BaseMonsterCard(baseCost, type, rarity, target, showInCardLibrary), IModRightClickableCard {

    protected virtual int RightClickCost => EnergyCost.GetAmountToSpend();
    
    protected virtual bool ShouldSpendResources => ClickType == RightClickType.Hand;
    
    protected abstract RightClickType ClickType { get; }
    
    public virtual async Task OnRightClick(ModRightClickExecutionContext context) {
        if (!TryValidateRightClick(context)) return;
        if (ShouldSpendResources) await SpendResources();
        await OnYgoRightClick(context);
    }

    // 入口只判断是否接收这次尝试，玩法条件留到执行阶段反馈。
    public bool CanExecuteRightClick(ModRightClickExecutionContext context) {
        if (context.Player != Owner || context.PlayerChoiceContext == null) return false;
        return ClickType switch {
            RightClickType.Hand => Pile?.Type == PileType.Hand,
            RightClickType.Graveyard => Pile?.Type == PileType.Discard,
            _ => false
        };
    }

    // 返回 null 表示允许发动；检查本身不得扣费、记录次数或显示提示。
    protected virtual LocString? ValidateRightClick(ModRightClickExecutionContext context) {
        return RightClickCost > Owner.GetEnergy() ? RightClickError("ENERGY") : null;
    }

    protected bool TryValidateRightClick(ModRightClickExecutionContext context) {
        if (!CanExecuteRightClick(context)) return false;
        LocString? error = ValidateRightClick(context);
        if (error == null) return true;
        ShowRightClickError(context, error);
        return false;
    }

    protected void ShowRightClickError(ModRightClickExecutionContext context, LocString error) {
        // 同步行动在各端执行，失败提示只展示给操作拥有者。
        if (!LocalContext.IsMe(context.Player)) return;
        RitsuToastService.ShowWarning(error.GetFormattedText(),
            new LocString("combat_messages", "USE_EFFECT_ERROR.title").GetFormattedText());
    }

    protected static LocString RightClickError(string reason) =>
        new("combat_messages", $"USE_EFFECT_ERROR_{reason}.body");

    protected virtual Task OnYgoRightClick(ModRightClickExecutionContext context) { return Task.CompletedTask; }
}

public enum RightClickType {
    Hand, //手发
    Graveyard, //墓效
}
