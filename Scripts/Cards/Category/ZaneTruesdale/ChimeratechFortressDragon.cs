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
public class ChimeratechFortressDragon()
    : BaseContactFusionCard(-1, CardRarity.Common, TargetType.None) {
    public override int CardId => 79229522;

    public override int BaseAttackVar => 1;
    public override int BaseLifeVar => 1;

    public override int MinFusionMaterialCount => 2;
    public override int? MaxFusionMaterialCount => null;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new AttackVar("BoostAttack", 3)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        YgoHoverTipConst.EnterField(),
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
        if (context.SummonedCreature.Monster is not ChimeratechFortressDragonMinion minion) {
            Entry.Logger.Error("Chimeratech Fortress Dragon fusion summoned without its minion model.");
            return;
        }

        await minion.ResolveFusionSummonEffect(
            context.ChoiceContext,
            context.Owner,
            this,
            context.Materials.Count,
            DynamicVars["BoostAttack"].IntValue
        );
    }

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars["BoostAttack"].UpgradeValueBy(1);
    }
}
