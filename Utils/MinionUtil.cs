using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Commands;
using MinionLib.Minion;
using VYgo.Scripts.Monsters;

namespace VYgo.Utils;

public static class MinionUtil {

    public const int MAX_MINION_COUNT = 5;
    
    public static int MinionCount(this Player player) {
        return player.Creature.Pets.Count(c => c.Monster is MinionModel);
    }
    
    public static async Task<Creature> AddMinionInstant<T>(
        PlayerChoiceContext choiceContext,
        Player player,
        MinionSummonOptions options = default (MinionSummonOptions))
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