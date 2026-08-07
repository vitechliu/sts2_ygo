using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using VYgo.Scripts.Monsters;

namespace VYgo.Core.Hooks;

/// <summary>
/// Optional listener for VYgo monsters destroyed by an opposing powered attack.
/// </summary>
public interface IMonsterBattleDestroyedHookListener {
    Task AfterMonsterBattleDestroyed(
        PlayerChoiceContext choiceContext,
        Creature destroyedCreature,
        Creature source) {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Tracks lethal attack damage until death prevention has resolved, then dispatches
/// the confirmed battle-destruction event to the current combat hook listeners.
/// </summary>
public static class MonsterBattleDestroyedHook {
    private static readonly AsyncLocal<Dictionary<Creature, Creature>?> PendingSources = new();

    internal static void RecordPotentialBattleDestruction(
        DamageResult result,
        ValueProp props,
        Creature? source) {
        Creature destroyedCreature = result.Receiver;
        if (!result.WasTargetKilled
            || destroyedCreature.Monster is not BaseMonster
            || source == null
            || source.Side == destroyedCreature.Side
            || !props.IsPoweredAttack()) {
            return;
        }

        var pendingSources = PendingSources.Value ??= [];
        pendingSources[destroyedCreature] = source;
    }

    internal static async Task AfterMonsterDeath(
        PlayerChoiceContext choiceContext,
        Creature destroyedCreature,
        bool wasRemovalPrevented) {
        if (!TryTakeSource(destroyedCreature, out Creature? source)
            || wasRemovalPrevented
            || destroyedCreature.CombatState is not { } combatState) {
            return;
        }

        foreach (AbstractModel model in combatState.IterateHookListeners()) {
            if (model is not IMonsterBattleDestroyedHookListener listener) continue;

            choiceContext.PushModel(model);
            try {
                await listener.AfterMonsterBattleDestroyed(
                    choiceContext,
                    destroyedCreature,
                    source);
                model.InvokeExecutionFinished();
            }
            finally {
                choiceContext.PopModel(model);
            }
        }
    }

    private static bool TryTakeSource(
        Creature destroyedCreature,
        out Creature? source) {
        Dictionary<Creature, Creature>? pendingSources = PendingSources.Value;
        if (pendingSources == null
            || !pendingSources.Remove(destroyedCreature, out source)) {
            source = null;
            return false;
        }

        if (pendingSources.Count == 0) {
            PendingSources.Value = null;
        }
        return true;
    }
}
