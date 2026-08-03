using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MinionLib.Targeting;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Powers;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberShield()
    : BaseEquipSpellCard(1, CardRarity.Common, MinionTargetTypes.AnyMinion) {
    public override int CardId => 63224564;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(5),
        new LifeVar(0)
    ];

    protected override async Task OnEquipped(
        PlayerChoiceContext choiceContext,
        Creature target) {
        await PowerCmd.Apply<AttackPower>(
            choiceContext,
            target,
            DynamicVars["Attack"].IntValue,
            Owner.Creature,
            this
        );
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
                this
            );
        }
    }

    protected override void OnUpgrade() {
        DynamicVars["Attack"].UpgradeValueBy(2m);
    }
}
