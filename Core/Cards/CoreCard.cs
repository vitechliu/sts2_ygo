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
}