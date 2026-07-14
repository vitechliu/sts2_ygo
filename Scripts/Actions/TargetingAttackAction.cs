using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Commands;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Actions;

public sealed class TargetingAttackAction : BasePerTurnMonsterAction {
    public override TargetType TargetType => TargetType.AnyEnemy;

    private int StrengthPowerAmount {
        get {
            var power = Owner.Powers.OfType<AttackPower>().FirstOrDefault();
            return power?.Amount ?? 0;
        }
    }

    public override bool CanAct(ICombatState combatState) {
        return base.CanAct(combatState) && StrengthPowerAmount > 0;
    }
    
    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target) {
        Triggered = true;
        if (target == null) return;
        await MinionAnimCmd.PlayBumpAttackAsync(Owner, target);
        await CreatureCmd.Damage(choiceContext, target, StrengthPowerAmount, ValueProp.Move, null, null);
    }
}
