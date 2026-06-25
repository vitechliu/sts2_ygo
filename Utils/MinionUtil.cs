using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Commands;
using MinionLib.Minion;

namespace VYgo.Utils;

public static class MinionUtil {

    public const int MAX_MINION_COUNT = 5;
    
    public static int MinionCount(this Player player) {
        return player.Creature.Pets.Count(c => c.Monster is MinionModel);
    }

    public static async Task<Creature> AddMinionInstant(
        Type monsterType,
        PlayerChoiceContext choiceContext,
        Player player,
        MinionSummonOptions options = default) {
        ArgumentNullException.ThrowIfNull(monsterType, nameof(monsterType));
        ArgumentNullException.ThrowIfNull(player, nameof(player));
        if (!typeof(MinionModel).IsAssignableFrom(monsterType))
            throw new ArgumentException($"Type {monsterType} must inherit from {nameof(MinionModel)}", nameof(monsterType));
        MethodInfo method = typeof(MinionUtil).GetMethod(nameof(AddMinionInstant), 1, new[] { typeof(PlayerChoiceContext), typeof(Player), typeof(MinionSummonOptions) })
                            ?? throw new InvalidOperationException("Generic AddMinionInstant method not found.");
        MethodInfo genericMethod = method.MakeGenericMethod(monsterType);
        return await (Task<Creature>)genericMethod.Invoke(null, new object[] { choiceContext, player, options });
    }
    
    public static async Task<Creature> AddMinionInstant<T>(
        PlayerChoiceContext choiceContext,
        Player player,
        MinionSummonOptions options = default)
        where T : MinionModel
    {
        ArgumentNullException.ThrowIfNull((object) player, nameof (player));
        Creature pet = await PlayerCmd.AddPet<T>(player);
        if (pet.Monster is MinionModel monster1)
            monster1.Position = options.Position;
        PetOrderSnapshotManager.TakeSnapshot(player);
        await MinionAnimCmd.Rearrange(false);
        if (pet.Monster is MinionModel monster2)
            await monster2.OnSummon(choiceContext, player, options);
        Creature creature = pet;
        pet = (Creature) null;
        return creature;
    }
}