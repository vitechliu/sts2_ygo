using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Minion;
using VYgo.Scripts.Actions;
using VYgo.Scripts.Powers;
using VYgo.Utils;

namespace VYgo.Scripts.Monsters.YGO;

public class ChimeratechOverdragonMinion : BaseMonster {
    public override int CardId => 64599569;

    public override bool BasicAttackAction => false;

    public async Task ResolveFusionSummonEffect(
        PlayerChoiceContext choiceContext,
        Player owner,
        CardModel source,
        int effectiveMaterialCount,
        int attackPerMaterial,
        int lifePerMaterial
    ) {
        List<Creature> otherMonsters = owner.Creature.Pets
            .Where(creature => creature != Creature
                && creature.IsAlive
                && creature.Monster is MinionModel)
            .ToList();

        foreach (var monster in otherMonsters) {
            await CreatureCmd.Kill(monster, true);
        }

        int attackIncrease = attackPerMaterial * effectiveMaterialCount;
        int lifeIncrease = lifePerMaterial * effectiveMaterialCount;
        await PowerCmd.Apply<AttackPower>(
            choiceContext,
            Creature,
            attackIncrease,
            owner.Creature,
            source
        );
        await MinionUtil.AddHp(Creature, lifeIncrease);
        await PowerCmd.Apply<ChimeratechOverdragonAttackAction>(
            choiceContext,
            Creature,
            effectiveMaterialCount,
            owner.Creature,
            source,
            true
        );
    }
}
