using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using VYgo.Core;
using VYgo.Scripts.Cards.Category.Fusion;
using VYgo.Scripts.Powers;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class HeosvarogtheMechanicalDawn()
    : BaseExtraFusionCard(-1, CardType.Skill, CardRarity.Rare, TargetType.None) {
    public override int CardId => 8963089;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 12;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new PowerVar<NegatingPower>(1m),
        new PowerVar<WeakPower>(1m),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        YgoHoverTipConst.EnterField(),
        YgoHoverTipConst.Action(),
        HoverTipFactory.FromCard<Polymerization>(),
        HoverTipFactory.FromPower<NegatingPower>(),
        HoverTipFactory.FromPower<WeakPower>(),
    ];

    public int NegatingAmount => DynamicVars["NegatingPower"].IntValue;
    public int WeakAmount => DynamicVars["WeakPower"].IntValue;

    public override bool CanUseFusionMaterial(SummonMaterial material) {
        return material.CoreCard.IsRace(YgoRace.Machine)
            && material.CoreCard?.Attribute == "光";
    }

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars["NegatingPower"].UpgradeValueBy(1m);
        DynamicVars["WeakPower"].UpgradeValueBy(1m);
    }
}
