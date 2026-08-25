using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Monsters.YGO;

public class PowercodeTalkerMinion: BaseMonster {
    public override int CardId => 15844566;

    public override async Task AfterAttack(PlayerChoiceContext choiceContext) {
        if (Creature.PetOwner is not { } owner) return;

        decimal attack = Creature.GetPowerAmount<AttackPower>();
        if (attack <= 0m) return;
        await PowerCmd.Apply<AttackPower>(
            choiceContext,
            Creature,
            attack,
            owner.Creature,
            SourceCard);
    }
}
