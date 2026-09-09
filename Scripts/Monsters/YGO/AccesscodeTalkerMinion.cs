using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;
namespace VYgo.Scripts.Monsters.YGO;
public class AccesscodeTalkerMinion : BaseMonster {
    public override int CardId => 86066372;
    public override bool BasicAttackAction => false;
    public override async Task OnSummonYgo(PlayerChoiceContext choiceContext, Player owner, MinionSummonOptions options) =>
        await ApplyMonsterAction<AccesscodeTalkerAction>(choiceContext, Creature, 1, owner.Creature, options.Source, true);
}
