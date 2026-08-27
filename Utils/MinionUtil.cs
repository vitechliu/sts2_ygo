using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Commands;
using MinionLib.Minion;
using VYgo.Scripts;
using VYgo.Scripts.Monsters;
using VYgo.Scripts.Powers;

namespace VYgo.Utils;

public static class MinionUtil {

    public const int MaxMinionCount = 5;

    public static int GetMaxMinionCount(this Player player) {
        int reduction = player.Creature.GetPowerAmount<MinionCapacityReductionPower>();
        return Math.Max(0, MaxMinionCount - reduction);
    }

    public static async Task AddHp(Creature creature, int amount) {
        if (amount <= 0) return;
        await CreatureCmd.SetMaxHp(creature, creature.MaxHp + amount);
        await CreatureCmd.Heal(creature, amount, false);
    }
    
    public static int MinionCount(this Player player) {
        return player.Creature.Pets.Count(c => c.Monster is MinionModel);
    }

    /// <summary>
    /// 将场上怪兽按其唯一来源卡映射到 Creature。共享同一来源卡的异常怪兽会整组忽略，
    /// 防止选择类效果因 ToDictionary 重复键而中断。
    /// </summary>
    public static Dictionary<CardModel, Creature> ToUniqueSourceCardTargets(
        this IEnumerable<Creature> creatures,
        string selectionContext
    ) {
        List<IGrouping<CardModel, Creature>> groups = creatures
            .Where(creature => creature.Monster is BaseMonster { SourceCard: not null })
            .GroupBy(creature => ((BaseMonster)creature.Monster!).SourceCard!)
            .ToList();

        foreach (IGrouping<CardModel, Creature> duplicateGroup in groups
                     .Where(group => group.Count() > 1)) {
            Entry.Logger.Error(
                $"{selectionContext} ignored {duplicateGroup.Count()} field monsters sharing " +
                $"source card {duplicateGroup.Key}."
            );
        }

        return groups
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single());
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
        if (genericMethod.Invoke(null, new object[] { choiceContext, player, options }) is not Task<Creature> task) {
            throw new InvalidOperationException("Generic AddMinionInstant returned an unexpected result.");
        }

        return await task;
    }
    
    public static async Task<Creature> AddMinionInstant<T>(
        PlayerChoiceContext choiceContext,
        Player player,
        MinionSummonOptions options = default)
        where T : MinionModel
    {
        ArgumentNullException.ThrowIfNull(player);
        Creature pet = await PlayerCmd.AddPet<T>(player);
        if (pet.Monster is MinionModel monster1)
            monster1.Position = options.Position;
        PetOrderSnapshotManager.TakeSnapshot(player);
        await MinionAnimCmd.Rearrange(false);
        if (pet.Monster is MinionModel monster2)
            await monster2.OnSummon(choiceContext, player, options);
        return pet;
    }
}
