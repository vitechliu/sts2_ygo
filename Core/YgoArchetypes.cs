namespace VYgo.Core;

/// <summary>
/// Stable numeric identifier for a YGO archetype/setname.
/// </summary>
public readonly record struct YgoArchetypeCode(ushort Value) {
    public override string ToString() => $"0x{Value:X4}";
}

/// <summary>
/// Readable aliases for archetypes used by card behavior code.
/// Card membership itself is loaded from VYgo/db.json.
/// </summary>
public static class YgoArchetypes {
    public static readonly YgoArchetypeCode Cyber = new(0x0093); // 电子
    public static readonly YgoArchetypeCode CyberDragon = new(0x1093); // 电子龙
    public static readonly YgoArchetypeCode Cyberdark = new(0x4093); // 电子暗黑
    public static readonly YgoArchetypeCode Spright = new(0x0180); // 卫星闪灵
    public static readonly YgoArchetypeCode Fiendsmith = new(0x01B0); // 刻魔
}
