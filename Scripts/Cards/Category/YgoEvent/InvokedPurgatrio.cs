using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.YgoEvent;

[RegisterCard(typeof(YgoEventCardPool))]
public class InvokedPurgatrio()
    : BaseExtraFusionCard(-1, CardType.Skill, CardRarity.Event, TargetType.None) {
    private const string MaterialAttribute = "炎";

    public override int CardId => 12307878;
    public override int BaseAttackVar => 10;
    public override int BaseLifeVar => 8;
    public override int UpgradeAttackVar => 4;

    public int EnemyBoostAttack => DynamicVars["EnemyBoostAttack"].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new AttackVar("EnemyBoostAttack", 8)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        YgoKeywords.Piercing
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        HoverTipFactory.FromCard<AleistertheInvoker>(),
        YgoHoverTipConst.Enhance()
    ];

    public override bool CanUseFusionMaterial(SummonMaterial material) {
        return InvokedFusionUtil.CanUseMaterial(material, MaterialAttribute);
    }

    public override bool HasValidFusionMaterials(IReadOnlyList<SummonMaterial> materials) {
        return base.HasValidFusionMaterials(materials)
            && InvokedFusionUtil.HasAleisterAndAttribute(materials, MaterialAttribute);
    }

    protected override void OnUpgrade() {
        base.OnUpgrade();
    }
}
