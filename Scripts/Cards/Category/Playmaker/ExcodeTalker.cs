using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;
using VYgo.Core.Cards;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class ExcodeTalker() : BaseExtraLinkCard(-1, CardRarity.Common, TargetType.None) {
    public override int CardId => 40669071;

    public override int BaseAttackVar => 15;
    public override int BaseLifeVar => 15;
    public override int UpgradeAttackVar => 5;

    public override int GetLinkMaterialCount(CoreCard coreCard) => 2;

    public override bool CanUseLinkMaterial(SummonMaterial material) {
        return material.CoreCard.IsRace(YgoRace.Cyberse);
    }
}
