using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core.Summon;
using VYgo.Scripts.Cards;

namespace VYgo.Core.Hooks;

/// <summary>
/// Optional listener for VYgo monster summon hooks.
/// Power, relic, card, and other combat hook models can implement this interface.
/// </summary>
public interface IMonsterSummonHookListener {
    Task BeforeMonsterSummon(
        PlayerChoiceContext choiceContext,
        BaseMonsterCard card,
        CardPlay cardPlay,
        SummonContext summonContext) {
        return Task.CompletedTask;
    }

    Task AfterMonsterSummon(
        PlayerChoiceContext choiceContext,
        BaseMonsterCard card,
        CardPlay cardPlay,
        Creature summonedCreature,
        SummonContext summonContext) {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Dispatches VYgo monster summon hooks to the current combat hook listeners.
/// </summary>
public static class MonsterSummonHook {
    public static async Task BeforeMonsterSummon(
        ICombatState combatState,
        PlayerChoiceContext choiceContext,
        BaseMonsterCard card,
        CardPlay cardPlay,
        SummonContext summonContext) {
        foreach (var model in combatState.IterateHookListeners()) {
            if (model is not IMonsterSummonHookListener listener) continue;

            choiceContext.PushModel(model);
            try {
                await listener.BeforeMonsterSummon(choiceContext, card, cardPlay, summonContext);
                model.InvokeExecutionFinished();
            }
            finally {
                choiceContext.PopModel(model);
            }
        }
    }

    public static async Task AfterMonsterSummon(
        ICombatState combatState,
        PlayerChoiceContext choiceContext,
        BaseMonsterCard card,
        CardPlay cardPlay,
        Creature summonedCreature,
        SummonContext summonContext) {
        foreach (AbstractModel model in combatState.IterateHookListeners()) {
            if (model is not IMonsterSummonHookListener listener) continue;

            choiceContext.PushModel(model);
            try {
                await listener.AfterMonsterSummon(choiceContext, card, cardPlay, summonedCreature, summonContext);
                model.InvokeExecutionFinished();
            }
            finally {
                choiceContext.PopModel(model);
            }
        }
    }
}
