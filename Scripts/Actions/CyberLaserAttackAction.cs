using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using VYgo.Scripts.Monsters.YGO;

namespace VYgo.Scripts.Actions;

public sealed class CyberLaserAttackAction : TargetingAttackAction {
    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target) {
        await base.OnAct(choiceContext, target);
        if (target is not { IsAlive: true }
            || Owner.Monster is not CyberLaserDragonMinion minion) return;

        await PowerCmd.Apply<VulnerablePower>(
            choiceContext,
            target,
            minion.VulnerableAmount,
            Owner,
            minion.SourceCard);
    }
}
