using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberdarkEndDragon() : BaseExtraFusionCard(-1, CardRarity.Token, TargetType.None) {
    public override int CardId => 37542782;

    public override int BaseAttackVar => 30;
    public override int BaseLifeVar => 20;
    public override int UpgradeAttackVar => 10;

    // 「铠黑龙-电子暗黑龙」＋「电子终结龙」
    public override int FusionMaterialCount => 2;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        YgoHoverTipConst.Equip(),
        HoverTipFactory.FromCard<CyberdarkDragon>(),
        HoverTipFactory.FromCard<CyberEndDragon>()
    ];

    public override bool CanUseFusionMaterial(SummonMaterial material) {
        return material.Card is CyberdarkDragon or CyberEndDragon;
    }

    public override bool HasValidFusionMaterials(IReadOnlyList<SummonMaterial> materials) {
        return base.HasValidFusionMaterials(materials)
            && materials.Any(material => material.Card is CyberdarkDragon)
            && materials.Any(material => material.Card is CyberEndDragon);
    }
}
