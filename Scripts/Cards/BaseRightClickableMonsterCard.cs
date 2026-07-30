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
        if (RightClickCost <= Owner.GetEnergy()) return;
        await SpendResources();
        await OnYgoRightClick(context);
    }

    public bool CanExecuteRightClick(ModRightClickExecutionContext context) {
        return RightClickCost <= Owner.GetEnergy();
    }

    protected virtual Task OnYgoRightClick(ModRightClickExecutionContext context) { return Task.CompletedTask; }
}
