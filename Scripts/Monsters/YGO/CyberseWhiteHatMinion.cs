using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using VYgo.Core;
using VYgo.Scripts.Cards.Category.Playmaker;

namespace VYgo.Scripts.Monsters.YGO;

public class CyberseWhiteHatMinion: BaseMonster {
    public override int CardId => 46104361;

    public override async Task OnUsedAsLinkMaterial(
        PlayerChoiceContext choiceContext,
        Player owner,
        IReadOnlyList<SummonMaterial> materials
    ) {
        if (SourceCard is CyberseWhiteHat sourceCard) {
            await PowerCmd.Apply<WeakPower>(
                choiceContext,
                owner.Creature.CombatState.HittableEnemies,
                sourceCard.Weak,
                owner.Creature,
                sourceCard);
        }
    }
}
