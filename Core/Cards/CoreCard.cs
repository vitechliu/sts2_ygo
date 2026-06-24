namespace VYgo.Core.Cards;

public record CoreCard(
    int Id,
    int CardId,
    string? Name,
    string? CnName,
    string? EnName,
    string? Types,
    string? Description,
    int? Atk,
    int? Def,
    int? Level,
    string? Attribute,
    string? Race
) : IYgoId;