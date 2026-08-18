using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Monsters.YGO;

public class CodeTalkerMinion: BaseMonster {
    public override int CardId => 53413628;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not CodeTalker sourceCard) return;

        int linkMonsterCount = owner.Creature.Pets.Count(pet =>
            pet.Monster is BaseMonster { SourceCard: BaseExtraLinkCard });
        if (linkMonsterCount > 0) {
            await PowerCmd.Apply<AttackPower>(
                choiceContext,
                Creature,
                sourceCard.BoostAttack * linkMonsterCount,
                owner.Creature,
                sourceCard);
        }
    }
}
