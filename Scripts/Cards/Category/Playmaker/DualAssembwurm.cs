using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class DualAssembwurm() : BaseMonsterCard(2, CardType.Attack, CardRarity.Common, TargetType.None) {
    public override int CardId => 7445307;

    public override int BaseAttackVar => 9;
    public override int BaseLifeVar => 4;
    public override int UpgradeAttackVar => 3;

    public int Damage => DynamicVars.Damage.IntValue;

    public decimal Vulnerable => DynamicVars.Vulnerable.BaseValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new DamageVar(14m, ValueProp.Move),
        new PowerVar<VulnerablePower>(2m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.Action(),
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
    ];

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
