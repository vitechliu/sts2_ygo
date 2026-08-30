using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;

namespace VYgo.Scripts.Monsters.YGO;

public class CyberDragonNovaMinion: BaseMonster {
    public override int CardId => 58069384;
    
    public override bool BasicAttackAction => false;

    public override async Task OnSummonYgo(PlayerChoiceContext choiceContext, Player owner, MinionSummonOptions options) {
        if (options.PrimaryStatAmount is { } strength && strength > 0m)
            await ApplyMonsterAction<CyberDragonNovaAction>(choiceContext, Creature, strength, owner.Creature, options.Source,
                true);
    }
}
