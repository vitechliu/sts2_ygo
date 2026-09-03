using MegaCrit.Sts2.Core.Entities.Cards;
using MinionLib.Targeting;
using MinionLib.Targeting.Pets;
using MinionLib.Targeting.Utilities;

namespace VYgo.Core.Targeting;

/// <summary>
/// VYgo 怪兽行动使用的 MinionLib 自定义目标类型。
/// </summary>
public static class VYgoMinionTargetTypes {
    /// <summary>
    /// 任意存活敌人或同一玩家控制的其他存活怪兽，不包含行动拥有者自身。
    /// </summary>
    public static TargetType AnyEnemyOrOtherMinion { get; } =
        CustomTargetTypeManager.Register(
            new DifferenceTargetType(
                new UnionTargetType(
                    BuiltInTargetType.From(TargetType.AnyEnemy),
                    new AnyMinionTargetType()
                ),
                new ItselfTargetType()
            ),
            "VYgo",
            "AnyEnemyOrOtherMinion"
        );
}
