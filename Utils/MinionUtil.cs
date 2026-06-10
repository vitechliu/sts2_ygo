using MegaCrit.Sts2.Core.Entities.Players;
using MinionLib.Minion;
using VYgo.Scripts.Monsters;

namespace VYgo.Utils;

public static class MinionUtil {

    public const int MAX_MINION_COUNT = 5;
    
    public static int MinionCount(this Player player) {
        return player.Creature.Pets.Count(c => c.Monster is MinionModel);
    }
}