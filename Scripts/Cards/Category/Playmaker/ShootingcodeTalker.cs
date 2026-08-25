using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;
using VYgo.Core.Cards;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class ShootingcodeTalker() : BaseExtraLinkCard(-1, CardRarity.Common, TargetType.None) {
    public override int CardId => 33897356;

    public override int BaseAttackVar => 8;
    public override int BaseLifeVar => 3;
    public override int UpgradeAttackVar => 2;

    public override int GetLinkMaterialCount(CoreCard coreCard) => 2;

    public override bool CanUseLinkMaterial(SummonMaterial material) {
        return material.CoreCard.IsRace(YgoRace.Cyberse);
    }
}
