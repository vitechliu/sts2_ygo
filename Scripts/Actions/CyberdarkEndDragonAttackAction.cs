using VYgo.Scripts;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Actions;

/// <summary>
/// 铠皇龙-电子暗黑终结龙的攻击：每装备1张卡，每回合可以额外攻击1次。
/// </summary>
public sealed class CyberdarkEndDragonAttackAction : TargetingAttackAction {
    protected override int MaxUses => 1 + Owner.GetPowerInstances<EquipmentPower>()
        .Count(power => power.EquipmentCard is { } card && card.Pile?.Type == Entry.EquipPile);
}
