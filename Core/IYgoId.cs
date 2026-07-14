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
        var res = Entry.CardYgoIdCache.GetValueOrDefault(entry.CardId);
        if (res == null) {
            Entry.Logger.Error("Cannot Find Ygo CardModel: " + entry.CardId);
        }
        return res;
    }
    public static BaseMonster? YgoGetMonster(this IYgoId entry) {
        var res = Entry.MonsterYgoIdCache.GetValueOrDefault(entry.CardId);
        if (res == null) {
            Entry.Logger.Error("Cannot Find Ygo Monster: " + entry.CardId);
        }
        return res;
    }
    public static CoreCard? YgoGetCore(this IYgoId entry) {
        var res = Entry.CoreCardCache.GetValueOrDefault(entry.CardId);
        if (res == null) {
            Entry.Logger.Error("Cannot Find Ygo Core Card: " + entry.CardId);
        }
        return res;
    }
}