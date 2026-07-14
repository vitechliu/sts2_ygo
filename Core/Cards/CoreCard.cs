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
    /// <summary>
    /// 用于界面展示的卡片类型信息，不包含攻击力、防御力和连接标记等战斗数值。
    /// </summary>
    public string FormatedInfo {
        get {
            if (string.IsNullOrWhiteSpace(Types)) return string.Empty;

            string info = NormalizeWhitespace(Types);
            int typeEnd = info.StartsWith('[') ? info.IndexOf(']') : -1;

            // 未知格式也不抛出异常，只移除能够明确识别的攻防数据。
            if (typeEnd < 0) return RemoveCombatStats(info);

            string type = info[..(typeEnd + 1)];
            string details = RemoveCombatStats(info[(typeEnd + 1)..].Trim());
            return details.Length == 0 ? type : $"{type}\n{details}";
        }
    }

    public int? LinkCount {
        get {
            if (Def == null) return null;
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
