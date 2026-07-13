using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.CyberDragon;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
[RegisterCharacterStarterCard(typeof(ZaneTruesdaleCharacter), 3)]
public class ProtoCyberDragon() : BaseMonsterCard(energyCost,rarity, targetType, shouldShowInCardLibrary) {
    public override int CardId => 26439287;

    public override YgoMaterialNames? MaterialCardName => YgoMaterialNames.电子龙;
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.NameAs(YgoMaterialNames.电子龙)
    ];
    
    private const int energyCost = 0;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    public override int BaseAttackVar => 3;
    public override int BaseLifeVar => 1;
    public override int UpgradeAttackVar => 1;
}
