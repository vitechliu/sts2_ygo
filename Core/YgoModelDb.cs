using VYgo.Scripts;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Monsters;

namespace VYgo.Core;

public class YgoModelDb {
    public static BaseVYgoCard? GetCard(int id) {
        return Entry.CardYgoIdCache.GetValueOrDefault(id);
    }
    public static BaseMonster? GetMonster(int id) {
        return Entry.MonsterYgoIdCache.GetValueOrDefault(id);
    }
}