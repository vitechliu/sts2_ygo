using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Playmaker;

namespace VYgo.Scripts.Monsters.YGO;

public class ShootingcodeTalkerMinion: BaseMonster {
    public override int CardId => 33897356;

    public override bool BasicAttackAction => false;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options) {
        if (options.Source is not ShootingcodeTalker sourceCard) return;

        int linkMonsterCount = owner.Creature.Pets.Count(pet =>
            pet.Monster is BaseMonster { SourceCard: BaseExtraLinkCard });
        await ApplyMonsterAction<TemporaryExtraAttackAction>(
            choiceContext,
            Creature,
            linkMonsterCount,
            owner.Creature,
            sourceCard,
            true);
    }

    public override async Task AfterAttack(PlayerChoiceContext choiceContext) {
        if (Creature.PetOwner is { } owner) {
            await CardPileCmd.Draw(choiceContext, 1, owner);
        }
    }
}
