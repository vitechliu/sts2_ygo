using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core;
using VYgo.Scripts.Monsters.YGO;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class ChimeratechOverdragon()
    : BaseExtraFusionCard(-1, CardRarity.Uncommon, TargetType.None) {
    public override int CardId => 64599569;

    public override int BaseAttackVar => 1;
    public override int BaseLifeVar => 1;

    public override int MinFusionMaterialCount => 2;
    public override int? MaxFusionMaterialCount => null;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new AttackVar("BoostAttack", 3),
        new LifeVar("BoostLife", 3),
        new DynamicVar("MaterialBonus", 0)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        YgoHoverTipConst.Enhance()
    ];

    public override bool CanUseFusionMaterial(SummonMaterial material) {
        return material.CoreCard?.IsRace(YgoRace.Machine) == true;
    }

    public override bool HasValidFusionMaterials(IReadOnlyList<SummonMaterial> materials) {
        return base.HasValidFusionMaterials(materials)
            && materials.Any(material => material.NameEquals(YgoMaterialNames.电子龙));
    }

    protected override async Task AfterFusionSummoned(SummonPostPlayContext context) {
        if (context.SummonedCreature.Monster is not ChimeratechOverdragonMinion minion) {
            Entry.Logger.Error("Chimeratech Overdragon fusion summoned without its minion model.");
            return;
        }

        int effectiveMaterialCount = context.Materials.Count
            + DynamicVars["MaterialBonus"].IntValue;
        await minion.ResolveFusionSummonEffect(
            context.ChoiceContext,
            context.Owner,
            this,
            effectiveMaterialCount,
            DynamicVars["BoostAttack"].IntValue,
            DynamicVars["BoostLife"].IntValue
        );
    }

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars["MaterialBonus"].UpgradeValueBy(1);
    }
}
