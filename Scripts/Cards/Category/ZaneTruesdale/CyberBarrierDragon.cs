using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;
using VYgo.Scripts.Characters;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
[RegisterCharacterStarterCard(typeof(ZaneTruesdaleCharacter), 2)]
public class CyberBarrierDragon() : BaseMonsterCard(1, CardRarity.Basic, TargetType.None) {
    public override int CardId => 68774379;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 5;
    public override int UpgradeAttackVar => 0;
    public override int UpgradeLifeVar => 2;
}
