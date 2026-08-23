using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;
using VYgo.Core.Cards;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(CommonCardPool))]
public class GiganticSpright() : BaseExtraXyzCard(-1, CardRarity.Event, TargetType.None) {
    public override int CardId => 54498517;

    public override int BaseAttackVar => 8;
    public override int BaseLifeVar => 4;
    public override int UpgradeAttackVar => 3;

    public override bool CanUseXyzMaterial(CoreCard coreCard, SummonMaterial material) {
        return material.IsField
            && material.Creature is { IsAlive: true }
            && YgoSummonRules.IsLevel2Rank2OrLink2(material.CoreCard);
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        YgoHoverTipConst.XYZMaterial(),
        YgoHoverTipConst.EnterField(),
        YgoHoverTipConst.SpecialSummon(),
        YgoHoverTipConst.Enhance()
    ];
}
