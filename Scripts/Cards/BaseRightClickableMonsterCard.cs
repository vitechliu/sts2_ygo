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

    protected virtual int RightClickCost => EnergyCost.GetAmountToSpend();
    
    protected virtual bool ShouldSpendResources => ClickType == RightClickType.Hand;
    
    protected abstract RightClickType ClickType { get; }
    
    public virtual async Task OnRightClick(ModRightClickExecutionContext context) {
        if (!CanExecuteRightClick(context)) return;
        if (ShouldSpendResources) await SpendResources();
        await OnYgoRightClick(context);
    }

    public virtual bool CanExecuteRightClick(ModRightClickExecutionContext context) {
        switch (ClickType) {
            case RightClickType.Hand:
                if (Pile?.Type != PileType.Hand) return false;
                break;
            case RightClickType.Graveyard:
                if (Pile?.Type != PileType.Discard) return false;  
                break;
        }
        return context.PlayerChoiceContext != null
            && RightClickCost <= Owner.GetEnergy();
    }

    protected virtual Task OnYgoRightClick(ModRightClickExecutionContext context) { return Task.CompletedTask; }
}

public enum RightClickType {
    Hand, //手发
    Graveyard, //墓效
}
