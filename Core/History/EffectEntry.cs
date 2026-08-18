using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Players;
using VYgo.Scripts.Cards;

namespace VYgo.Core.History;

public sealed class EffectEntry(
    Player player,
    int cardId,
    string sign,
    ICombatState combatState,
    CombatHistory history)
    : CombatHistoryEntry(
        player.Creature,
        combatState.RoundNumber,
        combatState.CurrentSide,
        history,
        combatState.Players), IYgoId {
    public Player Player { get; } = player;

    public int CardId { get; } = cardId;

    public string Sign { get; } = sign;

    public override string Description =>
        $"{Player.Character.Id.Entry} use {CardId}'s effect: {Sign}";
}

public static class EffectHistory {
    public static void RecordUseEffect(
        this CombatHistory history,
        IYgoId card,
        string sign,
        ICombatState combatState,
        Player player) {
        history.Add(
            combatState,
            new EffectEntry(player, card.CardId, sign, combatState, history));
    }
}
