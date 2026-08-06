using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Commands;
using MinionLib.Minion;
using VYgo.Scripts.Powers;

namespace VYgo.Patches;

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.GainBlock), typeof(Creature), typeof(decimal), typeof(ValueProp),
    typeof(CardPlay), typeof(bool))]
public static class YgoGuardianBlockToHpPatch {
    [HarmonyPrefix]
    private static bool Prefix(
        Creature creature,
        decimal amount,
        ValueProp props,
        CardPlay? cardPlay,
        bool fast,
        ref Task<decimal> __result
    ) {
        if (amount <= 0m || creature.GetPower<YgoPower>() is not { IsGuardian: true } || creature.IsDead) {
            return true;
        }

        __result = GainBlock(creature, amount, props, cardPlay, fast);
        return false;
    }

    public static async Task<decimal> GainBlock(
        Creature creature,
        decimal amount,
        ValueProp props,
        CardPlay? cardPlay,
        bool fast = false
    ) {
        if (CombatManager.Instance.IsOverOrEnding) return 0m;

        var combatState = creature.CombatState!;
        await Hook.BeforeBlockGained(combatState, creature, amount, props, cardPlay?.Card);
        var modifiedAmount = Hook.ModifyBlock(
            combatState,
            creature,
            amount,
            props,
            cardPlay?.Card,
            cardPlay,
            out var modifiers
        );
        modifiedAmount = Math.Max(modifiedAmount, 0m);
        await Hook.AfterModifyingBlockAmount(combatState, modifiedAmount, cardPlay?.Card, cardPlay, modifiers);
        if (modifiedAmount > 0m) {
            SfxCmd.Play("event:/sfx/block_gain");
            VfxCmd.PlayOnCreatureCenter(creature, "vfx/vfx_block");
            await CreatureCmd.SetMaxHp(creature, creature.MaxHp + modifiedAmount);
            await CreatureCmd.Heal(creature, modifiedAmount, false);
            CombatManager.Instance.History.BlockGained(combatState, creature, (int)modifiedAmount, props, cardPlay);
            if (fast) {
                await Cmd.CustomScaledWait(0.0f, 0.03f);
            }
            else {
                await Cmd.CustomScaledWait(0.1f, 0.25f);
            }
        }

        await Hook.AfterBlockGained(combatState, creature, modifiedAmount, props, cardPlay?.Card);
        return 0m;
    }
}

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage), typeof(PlayerChoiceContext),
    typeof(IEnumerable<Creature>), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel),
    typeof(CardPlay))]
public static class YgoGuardianOverkillPatch {
    private static readonly AsyncLocal<bool> IsHandling = new();
    public static readonly AsyncLocal<Creature?> SuppressedOwner = new();

    [HarmonyPrefix]
    private static bool Prefix(
        PlayerChoiceContext choiceContext,
        IEnumerable<Creature> targets,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay,
        ref Task<IEnumerable<DamageResult>> __result
    ) {
        if (IsHandling.Value) return true;

        var targetList = targets.ToList();
        if (targetList.Count != 1) return true;

        var target = targetList[0];
        if (!ShouldHandle(target, props)) return true;

        __result = HandleWithOverkillRedirect(choiceContext, targetList, amount, props, dealer, cardSource, cardPlay);
        return false;
    }

    private static bool ShouldHandle(Creature target, ValueProp props) {
        if (!target.IsPlayer || target.Player == null || target.IsDead || target.CombatState == null) return false;
        if (!props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered)) return false;

        return target.Pets.Any(pet => pet.IsAlive && IsFrontGuardian(pet));
    }

    private static async Task<IEnumerable<DamageResult>> HandleWithOverkillRedirect(
        PlayerChoiceContext choiceContext,
        IReadOnlyList<Creature> targets,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay
    ) {
        IsHandling.Value = true;
        try {
            var owner = targets[0];
            if (owner.Player == null || owner.CombatState == null) {
                return await CreatureCmd.Damage(choiceContext, targets, amount, props, dealer, cardSource, cardPlay);
            }

            var guardianOrder = PetOrderSnapshotManager.GetSnapshot(owner.Player, false)
                .Where(pet => IsFrontGuardian(pet) && pet.CombatId.HasValue)
                .Select(pet => pet.CombatId!.Value)
                .ToList();

            SuppressedOwner.Value = owner;
            List<DamageResult> initialResults;
            try {
                initialResults = (await CreatureCmd.Damage(
                    choiceContext,
                    targets,
                    amount,
                    props,
                    dealer,
                    cardSource,
                    cardPlay
                )).ToList();
            }
            finally {
                SuppressedOwner.Value = null;
            }

            var firstGuardianResult = initialResults.FirstOrDefault(result =>
                result.Receiver != owner
                && result.Receiver.PetOwner == owner.Player
                && (IsFrontGuardian(result.Receiver)
                    || result.Receiver.CombatId is uint receiverId && guardianOrder.Contains(receiverId))
            );

            if (firstGuardianResult is not { OverkillDamage: > 0 }
                || !firstGuardianResult.Receiver.CombatId.HasValue) {
                return initialResults;
            }

            List<DamageResult> redirectedResults = [];
            var remaining = firstGuardianResult.OverkillDamage;
            var firstGuardianId = firstGuardianResult.Receiver.CombatId.Value;
            var directProps = props | ValueProp.Unpowered;
            var firstGuardianIndex = guardianOrder.IndexOf(firstGuardianId);

            if (firstGuardianIndex < 0) {
                if (remaining > 0m) {
                    var ownerFinalFallback = (await CreatureCmd.Damage(
                        choiceContext,
                        [owner],
                        remaining,
                        directProps,
                        dealer,
                        cardSource,
                        cardPlay
                    )).FirstOrDefault() ?? new DamageResult(owner, directProps);
                    redirectedResults.Add(ownerFinalFallback);
                }

                initialResults.AddRange(redirectedResults);
                return initialResults;
            }

            foreach (var guardianId in guardianOrder.Skip(firstGuardianIndex + 1)) {
                if (remaining <= 0m) break;

                var defender = owner.CombatState.GetCreature(guardianId);
                if (defender is not { IsAlive: true } || !IsFrontGuardian(defender)) continue;

                var defenderResult = (await CreatureCmd.Damage(
                    choiceContext,
                    [defender],
                    remaining,
                    directProps,
                    dealer,
                    cardSource,
                    cardPlay
                )).FirstOrDefault() ?? new DamageResult(defender, directProps);
                redirectedResults.Add(defenderResult);
                remaining = defenderResult.OverkillDamage;
            }

            if (remaining > 0m) {
                var ownerFinal = (await CreatureCmd.Damage(
                    choiceContext,
                    [owner],
                    remaining,
                    directProps,
                    dealer,
                    cardSource,
                    cardPlay
                )).FirstOrDefault() ?? new DamageResult(owner, directProps);
                redirectedResults.Add(ownerFinal);
            }

            initialResults.AddRange(redirectedResults);
            return initialResults;
        }
        finally {
            IsHandling.Value = false;
        }
    }

    private static bool IsFrontGuardian(Creature creature) {
        return creature.GetPower<YgoPower>() is { IsGuardian: true }
               && (creature.Monster is not MinionModel minion || minion.Position == MinionPosition.Front);
    }
}

[HarmonyPatch(typeof(Creature), nameof(Creature.LoseHpInternal), typeof(decimal), typeof(ValueProp))]
public static class YgoGuardianOwnerDamageSuppressPatch {
    [HarmonyPrefix]
    private static bool Prefix(Creature __instance, decimal amount, ValueProp props, ref DamageResult __result) {
        var suppressedOwner = YgoGuardianOverkillPatch.SuppressedOwner.Value;
        if (suppressedOwner == null || __instance != suppressedOwner || amount <= 0m) return true;

        __result = new DamageResult(__instance, props);
        return false;
    }
}
