using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;
using VYgo.Scripts.Characters;

namespace VYgo.Scripts.Cards.Category.Synchro;

/// <summary>
/// 暂时用于测试，后续不动游星卡池上线后，可能会移动
/// </summary>
[RegisterCard(typeof(SynchroCardPool))]
[RegisterCharacterStarterCard(typeof(RedhatCharacter), 3)]
public class StardustDragon() : BaseExtraSynchroCard(-1, CardRarity.Common, TargetType.None) {
    public override int CardId => 44508094;

    public override int BaseAttackVar => 10;
    public override int BaseLifeVar => 10;
    
    public int TargetLevel { get; set; } = 8;
}
