using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(CommonCardPool))]
public class SprightJet() : BaseSprightMonsterCard(1, CardRarity.Event) {
    public override int CardId => 13533678;

    public override int BaseAttackVar => 5;
    public override int BaseLifeVar => 2;
    public override int UpgradeAttackVar => 2;
}
