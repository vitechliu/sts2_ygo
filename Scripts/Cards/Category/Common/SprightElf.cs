using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;
using VYgo.Core.Cards;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(EventCardPool))]
public class SprightElf() : BaseExtraLinkCard(-1, CardRarity.Event, TargetType.None) {
    public override int CardId => 27381364;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 7;
    public override int UpgradeLifeVar => 3;

    public override bool HasValidLinkMaterials(CoreCard coreCard, IReadOnlyList<SummonMaterial> materials) {
        return base.HasValidLinkMaterials(coreCard, materials)
            && materials.Any(material => YgoSummonRules.IsLevel2Rank2OrLink2(material.CoreCard));
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        YgoHoverTipConst.Action(),
        YgoHoverTipConst.SpecialSummon()
    ];
}
