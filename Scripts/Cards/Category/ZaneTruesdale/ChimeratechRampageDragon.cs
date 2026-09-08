using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Monsters.YGO;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class ChimeratechRampageDragon() : BaseExtraFusionCard(-1, CardType.Attack, CardRarity.Uncommon, TargetType.None) {
    public override int CardId => 84058253;
    public override int BaseAttackVar => 10;
    public override int BaseLifeVar => 3;
    public override int UpgradeAttackVar => 2;
    public override int MinFusionMaterialCount => 2;
    public override int? MaxFusionMaterialCount => null;
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar), new LifeVar(BaseLifeVar), new DamageVar(10, ValueProp.Unpowered),
        new CardsVar(2), new DynamicVar("StrengthLoss", 1)
    ];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips, YgoHoverTipConst.Action(), HoverTipFactory.FromPower<StrengthPower>()
    ];
    public override bool CanUseFusionMaterial(SummonMaterial material) =>
        material.VYgoCard?.ContainArchetype(YgoArchetypes.CyberDragon) == true;
    protected override async Task AfterFusionSummoned(SummonPostPlayContext context) {
        if (context.SummonedCreature.Monster is ChimeratechRampageDragonMinion minion) {
            await minion.ResolveFusionSummonEffect(context.ChoiceContext, context.Owner, this,
                context.Materials.Count * DynamicVars["StrengthLoss"].IntValue);
        }
    }
    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars.Damage.UpgradeValueBy(2);
    }
}
