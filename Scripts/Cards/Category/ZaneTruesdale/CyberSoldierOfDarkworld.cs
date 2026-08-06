using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;
using VYgo.Scripts.Characters;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
[RegisterCharacterStarterCard(typeof(ZaneTruesdaleCharacter), 2)]
public class CyberSoldierOfDarkworld() : BaseMonsterCard(1, CardRarity.Common, TargetType.None) {
    public override int CardId => 75559356;

    public override int BaseAttackVar => 3;
    public override int BaseLifeVar => 2;
    public override int UpgradeAttackVar => 1;
    public override int UpgradeLifeVar => 1;

    protected override YgoType CardYgoType => YgoType.normal;
}
