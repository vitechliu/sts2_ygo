using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Combat.Powers;
using STS2RitsuLib.Scaffolding.Content;

namespace VYgo.Scripts.Powers;

/// <summary>
/// 临时攻击力Power
/// </summary>
public abstract class TemporaryAttackPower<T> : ModTemporaryAppliedPowerTemplate<T, AttackPower>
    where T : AbstractModel
{
    // 类型，Buff或Debuff
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://images/powers/strength_power.png",
        BigIconPath: "res://images/powers/strength_power.png"
    );
    public override LocString Description => new("powers", "V_YGO_POWER_TEMPORARY_ATTACK_POWER.description");
}
