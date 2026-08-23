using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(CommonCardPool))]
public class SprightCarrot() : BaseSprightMonsterCard(1, CardRarity.Event) {
    public override int CardId => 2311090;

    public override int BaseAttackVar => 2;
    public override int BaseLifeVar => 7;

    public int Weak => DynamicVars["WeakPower"].IntValue;
    public int Vulnerable => DynamicVars["VulnerablePower"].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new PowerVar<WeakPower>(1m),
        new PowerVar<VulnerablePower>(1m)
    ];

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars["WeakPower"].UpgradeValueBy(1m);
        DynamicVars["VulnerablePower"].UpgradeValueBy(1m);
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        YgoHoverTipConst.Action(),
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>()
    ];
}
