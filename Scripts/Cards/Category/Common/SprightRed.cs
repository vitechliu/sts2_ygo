using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;
using VYgo.Scripts.Powers;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(CommonCardPool))]
public class SprightRed() : BaseSprightMonsterCard(1, CardRarity.Event) {
    public override int CardId => 75922381;

    public override int BaseAttackVar => 3;
    public override int BaseLifeVar => 6;
    public override int UpgradeLifeVar => 2;

    public int Negating => DynamicVars["NegatingPower"].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new PowerVar<NegatingPower>(1m)
    ];

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars["NegatingPower"].UpgradeValueBy(1m);
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        YgoHoverTipConst.EnterField(),
        HoverTipFactory.FromPower<NegatingPower>()
    ];
}
