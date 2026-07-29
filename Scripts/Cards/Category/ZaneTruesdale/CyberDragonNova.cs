using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;
using VYgo.Core.Cards;
using VYgo.Scripts.Characters;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
[RegisterCharacterStarterCard(typeof(ZaneTruesdaleCharacter), 1)]
public class CyberDragonNova() : BaseExtraXyzCard(1, CardRarity.Basic, TargetType.None, true) {
    public override int CardId => 58069384;
    public override int XyzMaterialCount => 2;
    public override int BaseAttackVar => 1;
    public override int BaseLifeVar => 1;
    public override int UpgradeAttackVar => 1;
    public override int UpgradeLifeVar => 1;

    // protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
    //     BaseSummonHoverTip,
    // ];
    
    public override bool CanUseXyzMaterial(CoreCard coreCard, SummonMaterial material) {
        return material.CoreCard?.Race == "机械族";
    }
}
