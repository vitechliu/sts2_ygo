using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.CyberDragon;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
[RegisterCharacterStarterCard(typeof(ZaneTruesdaleCharacter), 1)]
public class CyberTwinDragon() : BaseExtraFusionCard(energyCost, rarity, targetType, shouldShowInCardLibrary) {
    public override int CardId => 74157028;
    public override int FusionMaterialCount => 2;
    
    private const int energyCost = -1;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;


    // protected override IEnumerable<IHoverTip> ExtraHoverTips => new List<IHoverTip>();{
    //     HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
    // }


    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        YgoKeywords.Piercing
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(6),
        new LifeVar(4)
    ];
    
    protected override void OnUpgrade() {
        DynamicVars["Life"].UpgradeValueBy(2);
        DynamicVars["Attack"].UpgradeValueBy(2);
    }

    public override bool CanUseFusionMaterial(SummonMaterial material) {
        return material.VYgoCard?.MaterialCardName == YgoMaterialNames.电子龙;
    }
}
