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

public sealed class CyberKirinAction : BasePerTurnMonsterAction {
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Action(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromPower<IntangiblePower>(),
    ];

    protected override bool IsVisibleInternal => true;

    public override TargetType TargetType => TargetType.None;

    protected override string? IntentIconPath =>
        "res://images/powers/intangible_power.png";

    public override string? CustomIconPath => IntentIconPath;

    public override bool CanAct(ICombatState combatState) {
        return base.CanAct(combatState)
            && Owner.PetOwner != null
            && Owner.Monster is BaseMonster { SourceCard: not null };
    }

    protected override async Task OnAct(
        PlayerChoiceContext choiceContext,
        Creature? target) {
        var player = Owner.PetOwner;
        if (player == null
            || Owner.Monster is not BaseMonster cyberKirin
            || cyberKirin.SourceCard is not { } sourceCard
            || !cyberKirin.TryReserveSourceCardAsSummonMaterial(sourceCard)) {
            return;
        }

        SpendUses();
        await CardCmd.Exhaust(choiceContext, sourceCard);
        await CreatureCmd.Kill(Owner, true);
        await PowerCmd.Apply<IntangiblePower>(
            choiceContext,
            player.Creature,
            Amount,
            player.Creature,
            sourceCard);
    }
}
