using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;
namespace VYgo.Scripts.Monsters.YGO;
public class DegradeBusterMinion : BaseMonster {
    public override int CardId => 50426119;
    public override bool BasicAttackAction => false;
    public override async Task OnSummonYgo(PlayerChoiceContext choiceContext, Player owner, MinionSummonOptions options) =>
        await ApplyMonsterAction<DegradeBusterAction>(choiceContext, Creature, options.Source!.DynamicVars["Banish"].IntValue, owner.Creature, options.Source, true);
}
