using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Commands;
using VYgo.Core;
using VYgo.Core.Targeting;
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

    public override TargetType TargetType => VYgoMinionTargetTypes.AnyEnemyOrOtherMinion;

    protected override string? IntentIconPath => "res://images/packed/intents/intent_buff.png";
    public override string? CustomIconPath => IntentIconPath;

    protected override async Task OnAct(
        PlayerChoiceContext choiceContext,
        Creature? target
    ) {
        if (target == null) return;

        SpendUses();
        if (target.Side == CombatSide.Enemy) {
            int attack = Owner.Powers.OfType<AttackPower>().FirstOrDefault()?.Amount ?? 0;
            await MinionAnimCmd.PlayBumpAttackAsync(Owner, target);
            await CreatureCmd.Damage(choiceContext, target, attack, ValueProp.Move, null, null);
            if (Owner.Monster is BaseMonster monster) {
                await monster.AfterAttack(choiceContext);
            }
            return;
        }

        CardModel? sourceCard = (Owner.Monster as BaseMonster)?.SourceCard;
        await PowerCmd.Apply<CyberDragonSiegerTemporaryAttackPower>(
            choiceContext,
            target,
            Amount,
            Owner,
            sourceCard
        );
    }
}
