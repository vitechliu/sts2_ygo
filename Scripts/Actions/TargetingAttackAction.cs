using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Commands;
using VYgo.RitsuAdapters;

namespace VYgo.Scripts.Actions;

public sealed class TargetingAttackAction : ModActionTemplate {
    protected override bool IsVisibleInternal => false;
    public override TargetType TargetType => TargetType.AnyEnemy;
    public override bool AutoRemoveAtTurnEnd => false;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    private bool _attacked;

    private int StrengthPowerAmount => Owner.Powers.OfType<StrengthPower>().Count();

    public override bool CanAct(ICombatState combatState) {
        Creature owner = Owner;
        return !_attacked && Amount > 0M && owner.IsAlive && owner.CombatState == combatState && StrengthPowerAmount > 0;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        foreach (var creature in participants) {
            if (creature.IsPet && creature == Owner) {
                _attacked = false;
                return;
            }
        }
    }
    
    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target) {
        _attacked = true;
        if (target == null) return;
        await MinionAnimCmd.PlayBumpAttackAsync(Owner, target);
        await CreatureCmd.Damage(choiceContext, target, StrengthPowerAmount, ValueProp.Move, null, null);
    }
}