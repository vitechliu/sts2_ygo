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

    protected bool _triggered;

    public bool Triggered {
        get => _triggered;
        set {
            if (_triggered == value) return;

            _triggered = value;
            RefreshActionReadyIndicator();
        }
    }

    public override bool CanAct(ICombatState combatState) {
        Creature owner = Owner;
        return !_triggered && Amount > 0M && owner.IsAlive && owner.CombatState == combatState;
    }

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        foreach (var creature in participants) {
            if (creature.IsPet && creature == Owner) {
                Triggered = false;
                break;
            }
        }
        return Task.CompletedTask;
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource) {
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
        visuals?.SetActionReadyIndicatorVisible(HasUnusedPerTurnAction(owner));
    }

    private static bool HasUnusedPerTurnAction(Creature owner) {
        return owner.IsAlive &&
               owner.Powers
                   .OfType<BasePerTurnMonsterAction>()
                   .Any(action => !action.Triggered && action.Amount > 0);
    }
}
