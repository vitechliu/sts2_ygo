using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;
using VYgo.Core;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Scripts.Monsters;

namespace VYgo.Scripts.Actions;

public sealed class FlickClownAction : BasePerTurnMonsterAction {
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Action()
    ];

    protected override bool IsVisibleInternal => true;
    public override TargetType TargetType => TargetType.None;
    protected override string? IntentIconPath => "res://VYgo/images/powers/ygo.png";
    public override string? CustomIconPath => IntentIconPath;

    public override bool CanAct(ICombatState combatState) {
        return base.CanAct(combatState)
            && Owner.PetOwner != null
            && Owner.Monster is BaseMonster { SourceCard: FlickClown };
    }

    protected override async Task OnAct(
        PlayerChoiceContext choiceContext,
        Creature? target
    ) {
        if (Owner.PetOwner is not { } player
            || Owner.Monster is not BaseMonster { SourceCard: FlickClown sourceCard }) {
            return;
        }

        SpendUses();
        await CreatureCmd.Damage(
            choiceContext,
            player.Creature,
            1m,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
            sourceCard,
            null);
        await CardPileCmd.Draw(choiceContext, sourceCard.Draw, player);
    }
}
