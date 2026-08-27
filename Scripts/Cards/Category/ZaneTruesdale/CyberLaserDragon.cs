using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using VYgo.Core;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberLaserDragon() : BaseMonsterCard(2, CardType.Attack, CardRarity.Token, TargetType.None) {
    public override int CardId => 4162088;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new PowerVar<VulnerablePower>(2m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        HoverTipFactory.FromPower<VulnerablePower>()
    ];

    public override int BaseAttackVar => 10;
    public override int BaseLifeVar => 5;
    public override int UpgradeAttackVar => 2;
    public override int UpgradeLifeVar => 2;
}
