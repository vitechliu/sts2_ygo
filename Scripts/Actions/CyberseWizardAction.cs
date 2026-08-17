using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using VYgo.Core;
using VYgo.Scripts.Monsters;

namespace VYgo.Scripts.Actions;

public sealed class CyberseWizardAction : BasePerTurnMonsterAction {
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Action(),
        HoverTipFactory.FromPower<WeakPower>(),
    ];

    protected override bool IsVisibleInternal => true;

    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override string? IntentIconPath => "res://VYgo/images/powers/ygo.png";
    public override string? CustomIconPath => IntentIconPath;

    public override bool CanAct(ICombatState combatState) {
        return base.CanAct(combatState)
            && Owner.Monster is BaseMonster { SourceCard: Cards.Category.Playmaker.CyberseWizard };
    }

    protected override async Task OnAct(
        PlayerChoiceContext choiceContext,
        Creature? target
    ) {
        if (target == null
            || Owner.Monster is not BaseMonster { SourceCard: Cards.Category.Playmaker.CyberseWizard sourceCard }) {
            return;
        }

        SpendUses();
        await PowerCmd.Apply<WeakPower>(
            choiceContext,
            target,
            Amount,
            Owner,
            sourceCard);
    }
}
