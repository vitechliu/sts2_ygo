using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Ui.Toast;

namespace VYgo.Scripts.Cards;

public abstract class BaseRightClickableMonsterCard(
    int baseCost,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : BaseMonsterCard(baseCost, rarity, target, showInCardLibrary), IModRightClickableCard {

    protected virtual int RightClickCost => EnergyCost.GetAmountToSpend();
    
    protected virtual bool ShouldSpendResources => ClickType == RightClickType.Hand;
    
    protected abstract RightClickType ClickType { get; }
    
    public virtual async Task OnRightClick(ModRightClickExecutionContext context) {
        if (!CanExecuteRightClick(context, true)) return;
        if (ShouldSpendResources) await SpendResources();
        await OnYgoRightClick(context);
    }

    public virtual bool CanExecuteRightClick(ModRightClickExecutionContext context) {
        return CanExecuteRightClick(context, false);
    }
    public virtual bool CanExecuteRightClick(ModRightClickExecutionContext context, bool toast) {
        switch (ClickType) {
            case RightClickType.Hand:
                if (Pile?.Type != PileType.Hand) return false;
                break;
            case RightClickType.Graveyard:
                if (Pile?.Type != PileType.Discard) return false;  
                break;
        }

        if (RightClickCost > Owner.GetEnergy()) {
            if (toast) {
                RitsuToastService.ShowWarning(
                    new LocString("combat_messages", "USE_EFFECT_ERROR_ENERGY.body").GetFormattedText(),
                    new LocString("combat_messages", "USE_EFFECT_ERROR.title").GetFormattedText()
                );
            }
            return false;
        }
        return context.PlayerChoiceContext != null;
    }

    protected virtual Task OnYgoRightClick(ModRightClickExecutionContext context) { return Task.CompletedTask; }
}

public enum RightClickType {
    Hand, //手发
    Graveyard, //墓效
}
