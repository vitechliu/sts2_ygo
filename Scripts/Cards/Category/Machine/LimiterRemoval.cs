using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using VYgo.Core;
using VYgo.Scripts.Monsters;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Cards.Category.Machine;

[RegisterCard(typeof(MachineCardPool))]
public class LimiterRemoval() : BaseSpellCard(0, CardType.Skill, CardRarity.Rare, TargetType.None) {
    public override int CardId => 23171610;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        List<Creature> machineMonsters = Owner.Creature.Pets
            .Where(pet => pet.IsAlive
                && pet.Monster is BaseMonster monster
                && monster.YgoGetCore().IsRace(YgoRace.Machine))
            .ToList();

        foreach (var machineMonster in machineMonsters) {
            int currentAttack = machineMonster.GetPowerAmount<AttackPower>();
            if (currentAttack > 0) {
                await PowerCmd.Apply<LimiterRemovalTemporaryAttackPower>(
                    choiceContext,
                    machineMonster,
                    currentAttack,
                    Owner.Creature,
                    this);
            }
            await PowerCmd.Apply<SelfDestroyPower>(
                choiceContext,
                machineMonster,
                1m,
                Owner.Creature,
                this,
                true);
        }
    }

    protected override void OnUpgrade() {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
