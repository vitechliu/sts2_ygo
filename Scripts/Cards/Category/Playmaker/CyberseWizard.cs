using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class CyberseWizard() : BaseMonsterCard(1, CardRarity.Common, TargetType.None) {
    public override int CardId => 36033786;

    public override int BaseAttackVar => 4;
    public override int BaseLifeVar => 2;

    public decimal Weak => DynamicVars.Weak.BaseValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new PowerVar<WeakPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.Action(),
        HoverTipFactory.FromPower<WeakPower>(),
    ];

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars.Weak.UpgradeValueBy(1m);
    }
}
