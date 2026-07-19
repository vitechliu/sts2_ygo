using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Commands;

namespace VYgo.Scripts.Actions;

public class PenetratingAttackAction : TargetingAttackAction {
    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target) {
        SpendUses();
        if (target == null) return;
        await MinionAnimCmd.PlayBumpAttackAsync(Owner, target);
        await CreatureCmd.Damage(choiceContext, target, StrengthPowerAmount, ValueProp.Move | ValueProp.Unblockable, null, null);
    }
}
