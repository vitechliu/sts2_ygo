using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Commands;
using VYgo.Scripts.Monsters;

namespace VYgo.Scripts.Actions;

public class TargetingAttackAction : BasePerTurnMonsterAction {
    private const string AttackIntentIconDirectory = "res://images/packed/intents/attack";

    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override string? IntentIconPath => AttackDamage > 0
        ? $"{AttackIntentIconDirectory}/intent_attack_{GetAttackIntentTier(AttackDamage)}.png"
        : null;

    protected override int? IntentDamage => AttackDamage;

    protected override bool IntentIsAreaAttack => TargetType == TargetType.AllEnemies;

    protected virtual ValueProp DamageProps => ValueProp.Move;

    private int AttackDamage {
        get {
            var player = Owner.PetOwner ?? Owner.Player;
            if (player == null) return 0;

            decimal damage = Hook.ModifyDamage(
                player.RunState,
                Owner.CombatState,
                null,
                Owner,
                0m,
                DamageProps,
                null,
                null,
                ModifyDamageHookType.All,
                CardPreviewMode.None,
                out _);
            return (int)damage;
        }
    }

    public override bool CanAct(ICombatState combatState) {
        return base.CanAct(combatState) && AttackDamage > 0;
    }
    
    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target) {
        if (target == null) return;
        SpendUses();
        await MinionAnimCmd.PlayBumpAttackAsync(Owner, target);
        await CreatureCmd.Damage(
            choiceContext,
            target,
            0m,
            DamageProps,
            Owner,
            null,
            null);
        if (Owner.Monster is BaseMonster monster) {
            await monster.AfterAttack(choiceContext);
        }
    }

    private static int GetAttackIntentTier(int damage) {
        if (damage < 5) return 1;
        if (damage < 10) return 2;
        if (damage < 20) return 3;
        if (damage < 40) return 4;
        return 5;
    }
}
