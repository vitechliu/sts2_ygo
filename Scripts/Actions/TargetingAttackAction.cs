using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Commands;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Actions;

public class TargetingAttackAction : BasePerTurnMonsterAction {
    private const string AttackIntentIconDirectory = "res://images/packed/intents/attack";

    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override string IntentIconPath =>
        $"{AttackIntentIconDirectory}/intent_attack_{GetAttackIntentTier(StrengthPowerAmount)}.png";

    protected int StrengthPowerAmount {
        get {
            var power = Owner.Powers.OfType<AttackPower>().FirstOrDefault();
            return power?.Amount ?? 0;
        }
    }

    public override bool CanAct(ICombatState combatState) {
        return base.CanAct(combatState) && StrengthPowerAmount > 0;
    }
    
    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target) {
        SpendUses();
        if (target == null) return;
        await MinionAnimCmd.PlayBumpAttackAsync(Owner, target);
        await CreatureCmd.Damage(choiceContext, target, StrengthPowerAmount, ValueProp.Move, null, null);
    }

    private static int GetAttackIntentTier(int damage) {
        if (damage < 5) return 1;
        if (damage < 10) return 2;
        if (damage < 20) return 3;
        if (damage < 40) return 4;
        return 5;
    }
}
