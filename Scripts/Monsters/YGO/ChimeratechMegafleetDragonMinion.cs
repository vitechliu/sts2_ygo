using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Monsters.YGO;

public class ChimeratechMegafleetDragonMinion : BaseMonster {
    public override int CardId => 87116928;

    public async Task ResolveFusionSummonEffect(
        PlayerChoiceContext choiceContext,
        Player owner,
        CardModel source,
        int materialCount,
        int attackPerMaterial
    ) {
        await PowerCmd.Apply<AttackPower>(
            choiceContext,
            Creature,
            materialCount * attackPerMaterial,
            owner.Creature,
            source
        );
    }
}
