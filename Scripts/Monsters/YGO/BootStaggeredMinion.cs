using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;

namespace VYgo.Scripts.Monsters.YGO;

public class BootStaggeredMinion: BaseMonster {
    public override int CardId => 70950698;
    
    public override bool BasicAttackAction => false;

    public override async Task OnSummonYgo(PlayerChoiceContext choiceContext, Player owner, MinionSummonOptions options) {
        if (options.PrimaryStatAmount is { } strength && strength > 0m)
            await ApplyMonsterAction<BootStaggeredAttackAction>(choiceContext, Creature, strength, owner.Creature, options.Source,
                true);
    }
}
