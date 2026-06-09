using MegaCrit.Sts2.Core.Entities.Players;
using VYgo.Scripts.Monsters;

namespace VYgo.Utils;

public static class MinionUtil {
    public static int MinionCount(this Player player) {
        return 0;
        // return player.Creature.Pets.OfType<BaseMonster>().Count;
    }
}