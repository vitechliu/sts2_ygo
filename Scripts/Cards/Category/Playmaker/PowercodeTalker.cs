using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;
using VYgo.Core.Cards;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class PowercodeTalker() : BaseExtraLinkCard(-1, CardRarity.Common, TargetType.None) {
    public override int CardId => 15844566;

    public override int BaseAttackVar => 12;
    public override int BaseLifeVar => 1;
    public override int UpgradeAttackVar => 4;

    public override int GetLinkMaterialCount(CoreCard coreCard) => 2;

    public override bool CanUseLinkMaterial(SummonMaterial material) {
        return material.CoreCard.IsRace(YgoRace.Cyberse);
    }
}
