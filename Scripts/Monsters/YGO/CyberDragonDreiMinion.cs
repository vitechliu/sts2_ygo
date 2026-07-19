using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Core;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Monsters.YGO;

public class CyberDragonDreiMinion: BaseMonster {
    public override int CardId => 59281922;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options) {
        int targetLevel = options.Source is CyberDragonDrei drei
            ? drei.TargetLevel
            : CyberDragonDrei.BaseTargetLevel;
        List<Creature> cyberDragonMonsters = owner.Creature.Pets
            .Where(creature => creature.Monster is BaseMonster monster
                && monster.Level != null
                && monster.YgoGetCard()?.ContainArchetype(YgoArchetypes.CyberDragon) == true)
            .ToList();

        foreach (Creature monster in cyberDragonMonsters) {
            await MonsterLevelPower.SetLevel(
                choiceContext,
                monster,
                targetLevel,
                Creature,
                options.Source);
        }
    }
}
