using MegaCrit.Sts2.Core.ValueProps;

namespace VYgo.Scripts.Actions;

public class PenetratingAttackAction : TargetingAttackAction {
    protected override ValueProp DamageProps => ValueProp.Move | ValueProp.Unblockable;
}
