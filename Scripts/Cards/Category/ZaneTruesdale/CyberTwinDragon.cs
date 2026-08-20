using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
[RegisterCharacterStarterCard(typeof(ZaneTruesdaleCharacter), 1)]
public class CyberTwinDragon() : BaseExtraFusionCard(energyCost, rarity, targetType, shouldShowInCardLibrary) {
    public override int CardId => 74157028;
    public override int FusionMaterialCount => 2;
    
    private const int energyCost = -1;
    private const CardRarity rarity = CardRarity.Basic;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;


    // protected override IEnumerable<IHoverTip> ExtraHoverTips => new List<IHoverTip>();{
    //     HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
    // }


    public override int BaseAttackVar => 6;
    public override int BaseLifeVar => 5;
    public override int UpgradeAttackVar => 2;
    
    public override bool CanUseFusionMaterial(SummonMaterial material) {
        return material.NameEquals(YgoMaterialNames.电子龙);
    }
}
