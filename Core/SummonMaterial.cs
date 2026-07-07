using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core.Cards;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Monsters;

namespace VYgo.Core;

public sealed record SummonMaterial(CardModel? Card, Creature? Creature = null) {
    public bool IsField => Creature != null;
    public bool IsHand => Creature == null && Card?.Pile?.Type == PileType.Hand;

    public CoreCard? CoreCard {
        get {
            if (Card is IYgoId card) {
                return card.YgoGetCore();
            }

            if (Creature?.Monster is IYgoId monster) {
                return monster.YgoGetCore();
            }

            return null;
        }
    }

    public bool IsEffectMonster => CoreCard?.IsEffectMonster == true;

    public static bool IsFieldMonster(Creature creature) {
        return creature.Monster is BaseMonster;
    }

    public static bool IsHandMonsterCard(CardModel card) {
        return card is BaseMonsterCard { IsExtra: false } && card.Pile?.Type == PileType.Hand;
    }

    public static SummonMaterial FromFieldMonster(Creature creature) {
        CardModel? card = null;
        if (creature.Monster is IYgoId monster) {
            card = monster.YgoGetCard();
        }

        return new SummonMaterial(card, creature);
    }

    public static SummonMaterial FromHandMonsterCard(CardModel card) {
        return new SummonMaterial(card);
    }
}
