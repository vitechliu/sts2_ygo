using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core.Cards;
using VYgo.Scripts;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Monsters;

namespace VYgo.Core;

public sealed record SummonMaterial {
    private SummonMaterial(CardModel? card, Creature? creature, PileType sourcePile) {
        Card = card;
        Creature = creature;
        SourcePile = sourcePile;
    }

    public CardModel? Card { get; }
    public Creature? Creature { get; }

    /// <summary>
    /// The pile the material occupied when this snapshot was built. Field monsters use
    /// <see cref="Entry.MonsterPile"/> because their source cards live there.
    /// </summary>
    public PileType SourcePile { get; }

    public bool IsField => Creature != null;
    public bool IsHand => !IsField && SourcePile == PileType.Hand;

    public bool IsFromPile(PileType pileType) {
        return !IsField && SourcePile == pileType;
    }

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

    public BaseVYgoCard? VYgoCard {
        get {
            if (Card is BaseVYgoCard vYgoCard) return vYgoCard;
            if (Creature?.Monster is IYgoId monster) {
                return monster.YgoGetCard();
            }
            return null;
        }
    }

    public bool NameEquals(YgoMaterialNames name) {
        return (bool)VYgoCard?.NameEquals(name);
    }

    public int? CardId => CoreCard?.CardId;
    public string? CardName => CoreCard is { } coreCard
        ? new[] { coreCard.CnName, coreCard.Name, coreCard.EnName }.FirstOrDefault(name => !string.IsNullOrEmpty(name))
        : null;
    public string? Race => CoreCard?.Race;
    public bool IsEffectMonster => CoreCard?.IsEffectMonster == true;
    public int? Level => Creature?.Monster is BaseMonster monster
        ? monster.Level
        : (Card as BaseVYgoCard)?.Level;

    public static bool IsFieldMonster(Creature creature) {
        return creature.Monster is BaseMonster;
    }

    public bool IsTuner {
        get {
            if (VYgoCard is BaseMonsterCard monsterCard && monsterCard.ForceAsTuner) return true;
            return CoreCard?.IsTuner == true;
        }
    }

    public static bool IsHandMonsterCard(CardModel card) {
        return IsMonsterCardInPile(card, PileType.Hand);
    }

    public static bool IsMonsterCardInPile(CardModel card, PileType pileType) {
        if (card is not BaseMonsterCard monsterCard || card.Pile?.Type != pileType) {
            return false;
        }

        return pileType is PileType.Discard or PileType.Exhaust
            || !monsterCard.IsExtra;
    }

    public static SummonMaterial FromFieldMonster(Creature creature) {
        CardModel? card = (creature.Monster as BaseMonster)?.SourceCard;

        if (card == null) {
            Entry.Logger.Warn(
                $"Field monster {creature.Monster?.GetType().Name ?? creature.GetType().Name} " +
                "has no source combat card and cannot be selected as summon material."
            );
        }

        return new SummonMaterial(card, creature, Entry.MonsterPile);
    }

    public static SummonMaterial FromHandMonsterCard(CardModel card) {
        return FromMonsterCard(card);
    }

    public static SummonMaterial FromMonsterCard(CardModel card) {
        if (card.Pile is not { } pile) {
            throw new InvalidOperationException(
                $"Monster material {card.GetType().Name} is not in a pile."
            );
        }

        return new SummonMaterial(card, null, pile.Type);
    }
}
