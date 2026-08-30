namespace VYgo.Scripts.Actions;

/// <summary>
/// 怪兽行动传递给头顶 Intent UI 的只读展示状态。
/// UI 只消费此状态，不自行推断行动规则。
/// </summary>
public sealed record MonsterActionIntentState(
    bool Visible,
    string? IconPath,
    int? Damage,
    int RemainingUses,
    int MaxUses,
    bool IsSelectingTarget,
    bool IsAreaAttack
) {
    public static MonsterActionIntentState Hidden { get; } = new(
        false,
        null,
        null,
        0,
        0,
        false,
        false
    );
}
