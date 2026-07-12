using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.CyberDragon;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
[RegisterCharacterStarterCard(typeof(ZaneTruesdaleCharacter), 2)]
public class CyberDragonCore() : BaseMonsterCard(energyCost,rarity, targetType, shouldShowInCardLibrary) {
    public override int CardId => 23893227;
    
    public override List<YgoArchetypes> ArchetypesList => [YgoArchetypes.Cyber, YgoArchetypes.CyberDragon];

    private const int energyCost = 0;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 1;
    public override int UpgradeAttackVar => 0;
    public override int UpgradeLifeVar => 0;
}
