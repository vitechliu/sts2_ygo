using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(CommonCardPool))]
public class BlackLusterSoldier() : BaseRitualMonsterCard(0, CardRarity.Rare)
{
    public override int CardId => 5405694;
    public override int BaseAttackVar => 12;
    public override int BaseLifeVar => 10;
    public override int UpgradeAttackVar => 3;
    public override int UpgradeLifeVar => 3;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
}
