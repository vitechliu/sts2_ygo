using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Scripts.Monsters;

namespace VYgo.Scripts.Cards.Category.Common;

internal static class FiendsmithUtil {
    public static bool IsFiendsmithCard(CardModel card) {
        return card is BaseVYgoCard ygoCard
            && ygoCard.ContainArchetype(YgoArchetypes.Fiendsmith);
    }

    public static bool IsFiendsmithSpellTrap(CardModel card) {
        return card is BaseVYgoCard ygoCard
            && ygoCard.YgoCardType is YgoType.spell or YgoType.trap
            && ygoCard.ContainArchetype(YgoArchetypes.Fiendsmith);
    }

    public static bool IsLightFiendMonster(CardModel card) {
        return card is BaseMonsterCard monsterCard
            && IsLightFiend(monsterCard.YgoGetCore());
    }

    public static bool IsLightFiendMonster(SummonMaterial material) {
        return IsLightFiend(material.CoreCard);
    }

    public static bool IsLightFiendMonster(Creature creature) {
        return creature.Monster is BaseMonster monster
            && IsLightFiend(monster.YgoGetCore());
    }

    private static bool IsLightFiend(Core.Cards.CoreCard? coreCard) {
        return coreCard.IsRace(YgoRace.Fiend)
            && coreCard?.Attribute == "光";
    }
}
