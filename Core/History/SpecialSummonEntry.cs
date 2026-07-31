using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Players;

namespace VYgo.Core.History;

public sealed class SpecialSummonEntry(
    Player player,
    ICombatState combatState,
    CombatHistory history)
    : CombatHistoryEntry(
        player.Creature,
        combatState.RoundNumber,
        combatState.CurrentSide,
        history,
        combatState.Players) {
    public Player Player { get; } = player;

    public override string Description =>
        $"{Player.Character.Id.Entry} special summoned a YGO monster";
}

public static class SpecialSummonHistory {
    public static void RecordSpecialSummon(
        this CombatHistory history,
        ICombatState combatState,
        Player player) {
        history.Add(
            combatState,
            new SpecialSummonEntry(player, combatState, history));
    }
}
