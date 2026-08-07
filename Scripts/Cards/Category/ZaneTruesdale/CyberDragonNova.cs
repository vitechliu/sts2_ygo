using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;
using VYgo.Core.Cards;
using VYgo.Scripts.Characters;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
[RegisterCharacterStarterCard(typeof(ZaneTruesdaleCharacter), 1)]
public class CyberDragonNova() : BaseExtraXyzCard(1, CardRarity.Basic, TargetType.None, true) {
    public override int CardId => 58069384;
    public override int XyzMaterialCount => 2;
    public override int BaseAttackVar => 8;
    public override int BaseLifeVar => 5;
    public override int UpgradeAttackVar => 2;
    public override int UpgradeLifeVar => 2;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.XYZMaterial(),
        YgoHoverTipConst.SpecialSummon()
    ];
    
    public override bool CanUseXyzMaterial(CoreCard coreCard, SummonMaterial material) {
        return material.CoreCard.IsRace(YgoRace.Machine) && material.CoreCard.HasLevel && material.CoreCard.Level == 5;
    }
}
