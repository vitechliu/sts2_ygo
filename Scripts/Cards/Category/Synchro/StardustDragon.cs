using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;
using VYgo.Scripts.Characters;

namespace VYgo.Scripts.Cards.Category.Synchro;

[RegisterCard(typeof(SynchroCardPool))]
[RegisterCharacterStarterCard(typeof(RedhatCharacter), 3)]
public class StardustDragon() : BaseExtraSynchroCard(-1, CardRarity.Common, TargetType.None) {
    public override int CardId => 44508094;

    public override int BaseAttackVar => 1;
    public override int BaseLifeVar => 1;
    public override int UpgradeAttackVar => 1;
    public override int UpgradeLifeVar => 1;
    
    public int TargetLevel { get; set; } = 8;
}
