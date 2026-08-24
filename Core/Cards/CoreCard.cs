using System.Text.Json.Serialization;

namespace VYgo.Core.Cards;

public record CoreCard(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("card_id")]
    int CardId,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("cn_name")]
    string? CnName,
    [property: JsonPropertyName("en_name")]
    string? EnName,
    [property: JsonPropertyName("types")] string? Types,
    [property: JsonPropertyName("description")]
    string? Description,
    [property: JsonPropertyName("atk")] int? Atk,
    [property: JsonPropertyName("def")] int? Def,
    [property: JsonPropertyName("level")] int? Level,
    [property: JsonPropertyName("attribute")]
    string? Attribute,
    [property: JsonPropertyName("race")] string? Race
) : IYgoId {
    private IReadOnlyList<ushort> _archetypes = [];

    [JsonPropertyName("archetypes")]
    public IReadOnlyList<ushort> Archetypes {
        get => _archetypes;
        init => _archetypes = value ?? [];
    }

    /// <summary>
    /// 用于界面展示的卡片类型信息，不包含攻击力、防御力和连接标记等战斗数值。
    /// </summary>
    public string FormatedInfo => GetFormatedInfo(Level);

    public string GetFormatedInfo(int? effectiveLevel) {
        if (string.IsNullOrWhiteSpace(Types)) return string.Empty;

        string info = NormalizeWhitespace(Types);
        if (HasLevel && Level is { } originalLevel && effectiveLevel is { } currentLevel) {
            info = info.Replace($"[★{originalLevel}]", $"[★{currentLevel}]");
        }

        int typeEnd = info.StartsWith('[') ? info.IndexOf(']') : -1;

        // 未知格式也不抛出异常，只移除能够明确识别的攻防数据。
        if (typeEnd < 0) return RemoveCombatStats(info);

        string type = info[..(typeEnd + 1)];
        string details = RemoveCombatStats(info[(typeEnd + 1)..].Trim());
        return details.Length == 0 ? type : $"{type}\n{details}";
    }

    public bool HasLevel => Level is > 0 && Types?.Contains("[★") == true;

    public bool IsXyzMonster =>
        Types?.Contains("超量") == true
        || Types?.Contains("XYZ", StringComparison.OrdinalIgnoreCase) == true;

    public bool IsLinkMonster =>
        Types?.Contains("连接", StringComparison.Ordinal) == true
        || Types?.Contains("Link", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// 卡表中的调整标记可能来自中文 ygocdb 数据，也可能来自英文导入数据。
    /// 特殊的“视为调整”效果由同调怪兽卡的规则钩子处理，不应写回核心卡数据。
    /// </summary>
    public bool IsTuner =>
        Types?.Contains("调整", StringComparison.Ordinal) == true
        || Types?.Contains("Tuner", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// ygocdb stores an Xyz monster's Rank in the same numeric field used by monster Levels.
    /// Keep the distinction at the runtime-model boundary so Rank is never exposed as a normal Level.
    /// </summary>
    public int? Rank => IsXyzMonster ? Level : null;

    public int? LinkCount {
        get {
            // 非连接怪兽的 Def 是普通防御力，不能按连接标记位图解析。
            if (!IsLinkMonster || Def == null) return null;
            int linkCount = 0;
            for (int i = 0; i < 9; i++)
                if (((Def >> i) & 1u) > 0 && i != 4)
                    linkCount++;
            return linkCount;
        }
    }

    public bool IsEffectMonster => Types != null && Types.Contains("效果");

    private static string NormalizeWhitespace(string value) =>
        string.Join(' ', value.Split((char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static string RemoveCombatStats(string value) {
        int statsStart = FindCombatStatsStart(value);
        return (statsStart < 0 ? value : value[..statsStart]).TrimEnd();
    }

    private static int FindCombatStatsStart(string value) {
        for (int i = 0; i < value.Length; i++) {
            if (i > 0 && !char.IsWhiteSpace(value[i - 1])) continue;

            int cursor = i;
            if (!ReadStatComponent(value, ref cursor)) continue;

            SkipWhitespace(value, ref cursor);
            if (cursor >= value.Length || value[cursor] != '/') continue;
            cursor++;

            SkipWhitespace(value, ref cursor);
            if (!ReadStatComponent(value, ref cursor)) continue;

            if (cursor == value.Length || char.IsWhiteSpace(value[cursor])) return i;
        }

        return -1;
    }

    private static bool ReadStatComponent(string value, ref int cursor) {
        int start = cursor;
        while (cursor < value.Length && IsStatCharacter(value[cursor])) cursor++;
        return cursor > start;
    }

    private static bool IsStatCharacter(char value) =>
        char.IsDigit(value) || value is '?' or '-' or '∞';

    private static void SkipWhitespace(string value, ref int cursor) {
        while (cursor < value.Length && char.IsWhiteSpace(value[cursor])) cursor++;
    }
}
