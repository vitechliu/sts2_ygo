using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberLarva() : BaseMonsterCard(0, CardRarity.Common, TargetType.None) {
    public override int CardId => 35050257;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 4;

}
