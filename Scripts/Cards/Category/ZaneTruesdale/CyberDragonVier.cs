using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberDragonVier() : BaseMonsterCard(1, CardRarity.Uncommon, TargetType.None) {
    public override int CardId => 29975188;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new EnergyVar("CostReduction", 1)
    ];

    public override int BaseAttackVar => 3;
    public override int BaseLifeVar => 4;
    public override int UpgradeAttackVar => 1;
    public override int UpgradeLifeVar => 1;
}
