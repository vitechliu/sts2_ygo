using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Monsters.YGO;

public class DecodeTalkerExtendedMinion: BaseMonster {
    public override int CardId => 30822527;

    public override bool BasicAttackAction => false;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options) {
        if (options.Source is not DecodeTalkerExtended sourceCard) return;

        int linkMonsterCount = owner.Creature.Pets.Count(pet =>
            pet.Monster is BaseMonster { SourceCard: BaseExtraLinkCard });
        await PowerCmd.Apply<AttackPower>(
            choiceContext,
            Creature,
            sourceCard.BoostAttack * linkMonsterCount,
            owner.Creature,
            sourceCard);
        await ApplyMonsterAction<TemporaryExtraAttackAction>(
            choiceContext,
            Creature,
            linkMonsterCount,
            owner.Creature,
            sourceCard,
            true);
    }
}
