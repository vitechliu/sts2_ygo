using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(EventCardPool))]
public class SprightBlue() : BaseSprightMonsterCard(1, CardRarity.Event) {
    public override int CardId => 76145933;

    public override int BaseAttackVar => 4;
    public override int BaseLifeVar => 3;
    public override int UpgradeAttackVar => 2;
}
