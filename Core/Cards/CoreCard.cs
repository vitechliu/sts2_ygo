namespace VYgo.Core.Cards;

public abstract record CoreCard() : IYgoId {
    public abstract int CardId { get; }
}