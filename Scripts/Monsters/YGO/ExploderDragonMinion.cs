using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using VYgo.Core.Hooks;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;

namespace VYgo.Scripts.Monsters.YGO;

public class ExploderDragonMinion : BaseMonster, IMonsterBattleDestroyedHookListener {
    public override int CardId => 20586572;

    public async Task AfterMonsterBattleDestroyed(
        PlayerChoiceContext choiceContext,
        Creature destroyedCreature,
        Creature source) {
        if (destroyedCreature != Creature || source.IsDead) return;

        decimal damage = SourceCard?.DynamicVars.Damage.BaseValue
            ?? ExploderDragon.BaseDamage;
        await CreatureCmd.Damage(
            choiceContext,
            source,
            damage,
            ValueProp.Unpowered,
            dealer: null,
            cardSource: SourceCard,
            cardPlay: null);
    }
}
