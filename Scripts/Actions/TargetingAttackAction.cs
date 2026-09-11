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

    private int AttackDamage => PreviewMonsterDamage(props: DamageProps);

    public override bool CanAct(ICombatState combatState) {
        return base.CanAct(combatState) && AttackDamage > 0;
    }
    
    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target) {
        if (target == null) return;
        SpendUses();
        await MinionAnimCmd.PlayBumpAttackAsync(Owner, target);
        await DealMonsterDamage(choiceContext, target, props: DamageProps);
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
