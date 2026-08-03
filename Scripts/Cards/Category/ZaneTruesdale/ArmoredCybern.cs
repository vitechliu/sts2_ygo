using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core;
using VYgo.Scripts.Powers;
using VYgo.Scripts.Var;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class ArmoredCybern()
    : BaseMonsterCard(1, CardRarity.Uncommon, TargetType.None), IEquipmentEffect {
    public override int CardId => 67159705;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new AttackVar("EquipAttack", 0),
        new LifeVar("EquipLife", BaseLifeVar)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.Action(),
        YgoHoverTipConst.Equip(),
        YgoHoverTipConst.Enhance()
    ];

    public int EquipLife => DynamicVars["EquipLife"].IntValue;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 5;
    public override int UpgradeLifeVar => 2;

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars["EquipLife"].UpgradeValueBy(2m);
    }

    async Task IEquipmentEffect.OnEquipped(
        PlayerChoiceContext choiceContext,
        Creature target) {
        await MinionUtil.AddHp(target, EquipLife);
    }

    async Task IEquipmentEffect.OnUnequipped(
        PlayerChoiceContext choiceContext,
        Creature target) {
        if (!target.IsAlive) return;

        await CreatureCmd.LoseMaxHp(
            choiceContext,
            target,
            EquipLife,
            true
        );
    }
}
