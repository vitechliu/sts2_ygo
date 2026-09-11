using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Minion;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;

namespace VYgo.Scripts.Monsters.YGO;

public class CyberEltaninMinion : BaseMonster {
    public override int CardId => 33093439;

    public override async Task OnSummonYgo(PlayerChoiceContext choiceContext, Player owner, MinionSummonOptions options) {
        if (options.Source is not CyberEltanin source) return;
        var combat = Creature.CombatState;
        foreach (var other in owner.Creature.Pets.Where(p => p != Creature && p.IsAlive && p.Monster is MinionModel).ToList()) {
            await CreatureCmd.Kill(other, true);
        }
        if (combat == null) return;
        foreach (var enemy in combat.HittableEnemies.ToList()) {
            await CreatureCmd.Damage(choiceContext, enemy, source.DynamicVars["EnterDamage"].BaseValue,
                ValueProp.Move, owner.Creature, source, null);
        }
    }
}
