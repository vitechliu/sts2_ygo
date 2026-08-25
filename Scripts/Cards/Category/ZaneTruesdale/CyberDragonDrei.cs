using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using VYgo.Core;
using VYgo.Scripts.Powers;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberDragonDrei() : BaseMonsterCard(1, CardRarity.Common, TargetType.None) {
    public const int BaseTargetLevel = 5;

    public override int CardId => 59281922;

    public override YgoMaterialNames? MaterialCardName => YgoMaterialNames.电子龙;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new PowerVar<MonsterLevelPower>("Level", BaseTargetLevel),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.EnterField(),
        YgoHoverTipConst.NameAs(YgoMaterialNames.电子龙)
    ];

    public int TargetLevel => DynamicVars["Level"].IntValue;

    public override int BaseAttackVar => 5;
    public override int BaseLifeVar => 2;
    public override int UpgradeAttackVar => 1;
    public override int UpgradeLifeVar => 1;
}
