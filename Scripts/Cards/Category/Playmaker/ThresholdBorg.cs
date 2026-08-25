using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using VYgo.Core;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class ThresholdBorg() : BaseMonsterCard(3, CardRarity.Uncommon, TargetType.None) {
    public override int CardId => 31944175;

    public override int BaseAttackVar => 9;
    public override int BaseLifeVar => 5;

    public int StrengthLoss => DynamicVars["StrengthPower"].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new PowerVar<StrengthPower>(1m)
    ];

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars["StrengthPower"].UpgradeValueBy(1m);
    }
}
