using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib;
using VYgo.Scripts;

namespace VYgo.Core.Potions;

/// <summary>
/// VYgo 药水使用的自定义目标类型。
/// </summary>
public static class VYgoPotionTargetTypes {
    /// <summary>
    /// 任意存活的己方生物，包括玩家、己方怪兽及其他友方召唤物。
    /// </summary>
    public static TargetType AnyFriendlyCreature { get; } =
        RitsuLibFramework.RegisterSingleTargetType(
            Entry.ModId,
            "any_friendly_creature",
            static creature => creature is { IsAlive: true, Side: CombatSide.Player });
}
