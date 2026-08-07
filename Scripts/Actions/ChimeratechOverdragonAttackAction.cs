namespace VYgo.Scripts.Actions;

public sealed class ChimeratechOverdragonAttackAction : TargetingAttackAction {
    protected override int MaxUses => (int)Amount;
}
