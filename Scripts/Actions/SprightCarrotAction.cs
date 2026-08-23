using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using VYgo.Core;
using VYgo.Scripts.Cards.Category.Common;
using VYgo.Scripts.Monsters;

namespace VYgo.Scripts.Actions;

public sealed class SprightCarrotAction : BasePerTurnMonsterAction {
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Action(),
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>(),
    ];

    protected override bool IsVisibleInternal => true;

    public override TargetType TargetType => TargetType.None;

    protected override string? IntentIconPath => "res://VYgo/images/powers/ygo.png";
    public override string? CustomIconPath => IntentIconPath;

    protected override async Task OnAct(
        PlayerChoiceContext choiceContext,
        Creature? target
    ) {
        if (Owner.Monster is not BaseMonster { SourceCard: SprightCarrot sourceCard }) return;

        IReadOnlyList<Creature> enemies = Owner.CombatState.HittableEnemies;
        if (enemies.Count == 0) return;

        SpendUses();
        await PowerCmd.Apply<WeakPower>(
            choiceContext,
            enemies,
            sourceCard.Weak,
            Owner,
            sourceCard);
        await PowerCmd.Apply<VulnerablePower>(
            choiceContext,
            enemies,
            sourceCard.Vulnerable,
            Owner,
            sourceCard);
    }
}
