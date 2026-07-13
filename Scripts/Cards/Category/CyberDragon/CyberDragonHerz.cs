using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.CyberDragon;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
[RegisterCharacterStarterCard(typeof(ZaneTruesdaleCharacter), 2)]
public class CyberDragonHerz() : BaseMonsterCard(energyCost,rarity, targetType, shouldShowInCardLibrary) {
    public override int CardId => 56364287;
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.SendToGraveyard(),
        YgoHoverTipConst.NameAs(YgoMaterialNames.电子龙)
    ];
    
    public override YgoMaterialNames? MaterialCardName => YgoMaterialNames.电子龙;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new CardsVar(1),
    ];
    
    public override List<YgoArchetypes> ArchetypesList => [YgoArchetypes.Cyber, YgoArchetypes.CyberDragon];

    private const int energyCost = 0;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    public override int BaseAttackVar => 2;
    public override int BaseLifeVar => 1;

    protected override void OnUpgrade() {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
