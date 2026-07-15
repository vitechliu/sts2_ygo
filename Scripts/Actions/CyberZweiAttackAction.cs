using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using VYgo.Scripts.Monsters.YGO;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Actions;

public class CyberZweiAttackAction : TargetingAttackAction {
    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target) {
        await base.OnAct(choiceContext, target);
    
        CyberDragonZweiMinion m = Owner.Monster as CyberDragonZweiMinion;
        if (m == null) {
            Entry.Logger.Error("Cannot Find CyberDragonZweiMinion");
            return;
        }
        var val = m.Upgraded ? 4 : 3;
        await PowerCmd.Apply<AttackPower>(choiceContext, Owner, val, Owner, null);
    }
}
