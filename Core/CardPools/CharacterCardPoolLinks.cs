using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace VYgo.Core.CardPools;

public static class CharacterCardPoolLinks {
    private static readonly Dictionary<Type, List<Type>> ExtraPoolsByCharacter = [];

    public static void Register<TCharacter, TCardPool>()
        where TCharacter : CharacterModel
        where TCardPool : CardPoolModel {
        Register(typeof(TCharacter), typeof(TCardPool));
    }

    public static void Register(Type characterType, Type cardPoolType) {
        if (!typeof(CharacterModel).IsAssignableFrom(characterType)) {
            throw new ArgumentException($"{characterType.FullName} is not a character model.", nameof(characterType));
        }

        if (characterType.Assembly != typeof(CharacterCardPoolLinks).Assembly) {
            throw new ArgumentException(
                $"{characterType.FullName} is not defined by the VYgo assembly.",
                nameof(characterType));
        }

        if (!typeof(CardPoolModel).IsAssignableFrom(cardPoolType)) {
            throw new ArgumentException($"{cardPoolType.FullName} is not a card pool model.", nameof(cardPoolType));
        }

        if (!ExtraPoolsByCharacter.TryGetValue(characterType, out var pools)) {
            pools = [];
            ExtraPoolsByCharacter[characterType] = pools;
        }

        if (!pools.Contains(cardPoolType)) {
            pools.Add(cardPoolType);
        }
    }

    public static bool HasExtraPools(CharacterModel character) {
        if (character.GetType().Assembly != typeof(CharacterCardPoolLinks).Assembly) return false;
        return ExtraPoolsByCharacter.TryGetValue(character.GetType(), out var pools) && pools.Count > 0;
    }

    public static IReadOnlyList<CardPoolModel> GetPoolsFor(CharacterModel character) {
        var pools = new List<CardPoolModel> { character.CardPool };

        if (ExtraPoolsByCharacter.TryGetValue(character.GetType(), out var poolTypes)) {
            foreach (var poolType in poolTypes) {
                pools.Add(ModelDb.GetById<CardPoolModel>(ModelDb.GetId(poolType)));
            }
        }

        return pools.DistinctBy(static pool => pool.Id).ToList();
    }

    public static IEnumerable<CardModel> GetUnlockedCardsFor(Player player) {
        return GetUnlockedCardsFor(player, player.RunState.CardMultiplayerConstraint);
    }

    public static IEnumerable<CardModel> GetUnlockedCardsFor(
        Player player,
        CardMultiplayerConstraint multiplayerConstraint) {
        return GetPoolsFor(player.Character)
            .SelectMany(pool => pool.GetUnlockedCards(player.UnlockState, multiplayerConstraint))
            .DistinctBy(static card => card.Id);
    }
}
