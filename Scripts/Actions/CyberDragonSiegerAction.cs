using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Targeting;
using VYgo.Core;
using VYgo.Scripts.Monsters;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Actions;

public sealed class CyberDragonSiegerAction : BasePerTurnMonsterAction {
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Action(),
        YgoHoverTipConst.Enhance(),
        HoverTipFactory.FromPower<CyberDragonSiegerTemporaryAttackPower>()
    ];

    protected override bool IsVisibleInternal => true;

    public override TargetType TargetType => MinionTargetTypes.AnyMinion;

    protected override string? IntentIconPath => "res://images/powers/strength_power.png";
    public override string? CustomIconPath => IntentIconPath;

    protected override async Task OnAct(
        PlayerChoiceContext choiceContext,
        Creature? target
    ) {
        if (target == null) return;

        CardModel? sourceCard = (Owner.Monster as BaseMonster)?.SourceCard;
        SpendUses();
        await PowerCmd.Apply<CyberDragonSiegerTemporaryAttackPower>(
            choiceContext,
            target,
            Amount,
            Owner,
            sourceCard
        );
    }
}
