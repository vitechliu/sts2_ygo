using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using VYgo.Core;
using VYgo.Scripts.Monsters;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Actions;

public sealed class HeosvarogtheMechanicalDawnAction : BasePerTurnMonsterAction {
    protected override bool IsVisibleInternal => true;

    public override TargetType TargetType => TargetType.None;

    protected override string? IntentIconPath =>
        "res://VYgo/images/powers/negating_power.png";

    public override string? CustomIconPath => IntentIconPath;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Action(),
        HoverTipFactory.FromPower<NegatingPower>(),
        HoverTipFactory.FromPower<WeakPower>(),
    ];

    protected override async Task OnAct(
        PlayerChoiceContext choiceContext,
        Creature? target) {
        Player? player = Owner.PetOwner;
        if (player == null) return;

        CardModel? sourceCard = (Owner.Monster as BaseMonster)?.SourceCard;
        SpendUses();
        await PowerCmd.Apply<NegatingPower>(
            choiceContext,
            Owner,
            Amount,
            Owner,
            sourceCard);
        await PowerCmd.Apply<WeakPower>(
            choiceContext,
            Owner.CombatState.HittableEnemies,
            Amount,
            Owner,
            sourceCard);
    }
}
