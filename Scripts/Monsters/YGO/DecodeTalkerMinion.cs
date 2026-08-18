using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Monsters.YGO;

public class DecodeTalkerMinion: BaseMonster {
    public override int CardId => 1861629;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not DecodeTalker sourceCard) return;

        int linkMonsterCount = owner.Creature.Pets.Count(pet =>
            pet.Monster is BaseMonster { SourceCard: BaseExtraLinkCard });
        if (linkMonsterCount <= 0) return;

        await PowerCmd.Apply<AttackPower>(
            choiceContext,
            Creature,
            sourceCard.BoostAttack * linkMonsterCount,
            owner.Creature,
            sourceCard);
        await PowerCmd.Apply<NegatingPower>(
            choiceContext,
            Creature,
            sourceCard.Negating * linkMonsterCount,
            owner.Creature,
            sourceCard);
    }
}
