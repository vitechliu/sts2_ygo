using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Core.Cards;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
[RegisterCharacterStarterCard(typeof(ZaneTruesdaleCharacter), 1)]
public class CyberDragonInfinity()
    : BaseExtraXyzCard(energyCost, rarity, targetType, shouldShowInCardLibrary) {
    public override int CardId => 10443957;
    public override int XyzMaterialCount => 1;
    
    private const int energyCost = -1;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    public override int BaseAttackVar => 5;
    public override int BaseLifeVar => 4;
    public override int UpgradeAttackVar => 1;
    public override int UpgradeLifeVar => 1;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        HoverTipFactory.FromPower<NegatingPower>(),
        HoverTipFactory.FromCard<CyberDragonNova>()
    ];
    
    public override bool CanUseXyzMaterial(CoreCard coreCard, SummonMaterial material) {
        return material.Card is CyberDragonNova;
    }
}
