using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Scaffolding.Characters;

namespace VYgo.Scripts.Cards;

public abstract class BaseRightClickableMonsterCard(
    int baseCost,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : BaseMonsterCard(baseCost, rarity, target, showInCardLibrary), IModRightClickableCard {

    protected virtual int RightClickCost => baseCost;
    
    public virtual async Task OnRightClick(ModRightClickExecutionContext context) {
        if (!CanExecuteRightClick(context)) return;
        await SpendResources();
        await OnYgoRightClick(context);
    }

    public virtual bool CanExecuteRightClick(ModRightClickExecutionContext context) {
        return context.PlayerChoiceContext != null
            && RightClickCost <= Owner.GetEnergy();
    }

    protected virtual Task OnYgoRightClick(ModRightClickExecutionContext context) { return Task.CompletedTask; }
}
