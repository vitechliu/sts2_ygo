using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MinionLib.Targeting;
using VYgo.Scripts.Powers;
using VYgo.Scripts.Var;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.Machine;

[RegisterCard(typeof(MachineCardPool))]
public class MachineConversionFactory()
    : BaseEquipSpellCard(0, CardRarity.Common, MinionTargetTypes.AnyMinion) {
    public override int CardId => 25769732;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(3),
        new LifeVar(3),
    ];

    protected override async Task OnEquipped(
        PlayerChoiceContext choiceContext,
        Creature target) {
        await PowerCmd.Apply<AttackPower>(choiceContext, target, DynamicVars["Attack"].IntValue, Owner.Creature, this);
        await MinionUtil.AddHp(target, DynamicVars["Life"].IntValue);
    }

    protected override async Task OnUnequipped(
        PlayerChoiceContext choiceContext,
        Creature target) {
        if (!target.IsAlive) return;

        if (target.GetPower<AttackPower>() is { } attackPower) {
            await PowerCmd.ModifyAmount(
                choiceContext,
                attackPower,
                -DynamicVars["Attack"].IntValue,
                Owner.Creature,
                this);
        }

        await CreatureCmd.LoseMaxHp(
            choiceContext,
            target,
            DynamicVars["Life"].IntValue,
            true);
    }

    protected override void OnUpgrade() {
        DynamicVars["Attack"].UpgradeValueBy(1);
        DynamicVars["Life"].UpgradeValueBy(1);
    }
}
