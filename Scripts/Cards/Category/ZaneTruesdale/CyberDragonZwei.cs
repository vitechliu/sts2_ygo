using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using VYgo.Core;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberDragonZwei() : BaseMonsterCard(1, CardType.Attack, CardRarity.Common, TargetType.None) {
    public override int CardId => 5373478;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new DynamicVar("AttackAdd", 3m)
    ];
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        YgoHoverTipConst.NameAs(YgoMaterialNames.电子龙)
    ];
    
    public override YgoMaterialNames? MaterialCardName => YgoMaterialNames.电子龙;

    public override int BaseAttackVar => 4;
    public override int BaseLifeVar => 3;

    public override int UpgradeAttackVar => 1;

    // protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
    //     BaseSummonHoverTip,
    // ];

    protected override void OnUpgrade() {
        DynamicVars["AttackAdd"].UpgradeValueBy(1m);
    }
}
