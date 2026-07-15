using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.RitsuAdapters;

namespace VYgo.Scripts.Actions;

public abstract class BasePerTurnMonsterAction : ModActionTemplate {
    protected override bool IsVisibleInternal => false;
    public override bool AutoRemoveAtTurnEnd => false;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    protected abstract string IntentIconPath { get; }

    //每回合使用次数
    protected virtual int MaxUses => 1;
    //本回合剩余使用次数
    public int RemainingUses = -1;

    protected void SpendUses() {
        RemainingUses--;
        RefreshActionReadyIndicator();
    }
    protected void RecoverUses() {
        RemainingUses = MaxUses;
        RefreshActionReadyIndicator();
    }

    public override bool CanAct(ICombatState combatState) {
        Creature owner = Owner;
        return RemainingUses > 0 && Amount > 0M && owner.IsAlive && owner.CombatState == combatState;
    }

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        foreach (var creature in participants) {
            if (creature.IsPet && creature == Owner) {
                RecoverUses();
                break;
            }
        }
        return Task.CompletedTask;
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource) {
        RecoverUses();
        return Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource
    ) {
        if (power.Owner == Owner)
            RefreshActionReadyIndicator();
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner) {
        RefreshActionReadyIndicator(oldOwner);
        return Task.CompletedTask;
    }

    protected void RefreshActionReadyIndicator() {
        RefreshActionReadyIndicator(Owner);
    }

    private static void RefreshActionReadyIndicator(Creature owner) {
        var visuals = owner.GetCreatureNode()?.Visuals as NMonsterVisuals;
        var action = owner.IsAlive
            ? owner.Powers
                .OfType<BasePerTurnMonsterAction>()
                .FirstOrDefault(action => action.RemainingUses > 0 && action.Amount > 0)
            : null;
        visuals?.SetActionReadyIndicator(action?.IntentIconPath);
    }
}
