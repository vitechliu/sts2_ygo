using VYgo.Core.Cards;
using VYgo.Scripts;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Monsters;

namespace VYgo.Core;

public interface IYgoId {
    public int CardId { get; }
}

public static class IYgoIdHelper {
    public static BaseVYgoCard? YgoGetCard(this IYgoId entry) {
        return Entry.CardYgoIdCache.GetValueOrDefault(entry.CardId);
    }
    public static BaseMonster? YgoGetMonster(this IYgoId entry) {
        return Entry.MonsterYgoIdCache.GetValueOrDefault(entry.CardId);
    }
    public static CoreCard? YgoGetCore(this IYgoId entry) {
        return Entry.CoreCardCache.GetValueOrDefault(entry.CardId);
    }
}