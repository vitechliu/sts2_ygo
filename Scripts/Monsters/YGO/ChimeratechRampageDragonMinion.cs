using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Monsters.YGO;

public class ChimeratechRampageDragonMinion : BaseMonster {
    public override int CardId => 84058253;
    public override bool BasicAttackAction => false;
    public override async Task OnSummonYgo(PlayerChoiceContext choiceContext, Player owner, MinionSummonOptions options) {
        if (options.Source is not ChimeratechRampageDragon source) return;
        var action = await ApplyMonsterAction<ChimeratechRampageDragonAction>(
            choiceContext, Creature, 1, owner.Creature, source, true);
        if (action != null) {
            action.DynamicVars.Damage.BaseValue = source.DynamicVars.Damage.BaseValue;
            action.DynamicVars.Cards.BaseValue = source.DynamicVars.Cards.BaseValue;
        }
    }
    public async Task ResolveFusionSummonEffect(PlayerChoiceContext choiceContext, Player owner,
        ChimeratechRampageDragon source, int strengthLoss) {
        if (Creature.CombatState is not { } combat || strengthLoss <= 0) return;
        await PowerCmd.Apply<ChimeratechRampageDragonStrengthPower>(choiceContext,
            combat.HittableEnemies.ToList(), strengthLoss, owner.Creature, source);
    }
}
