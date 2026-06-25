using System.Text.Json.Serialization;

namespace VYgo.Core.Cards;

public record CoreCard(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("card_id")] int CardId,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("cn_name")] string? CnName,
    [property: JsonPropertyName("en_name")] string? EnName,
    [property: JsonPropertyName("types")] string? Types,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("atk")] int? Atk,
    [property: JsonPropertyName("def")] int? Def,
    [property: JsonPropertyName("level")] int? Level,
    [property: JsonPropertyName("attribute")] string? Attribute,
    [property: JsonPropertyName("race")] string? Race
) : IYgoId;