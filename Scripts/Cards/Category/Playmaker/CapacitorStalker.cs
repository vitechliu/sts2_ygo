using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using VYgo.Core;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class CapacitorStalker() : BaseMonsterCard(2, CardRarity.Common, TargetType.None) {
    public override int CardId => 29716911;

    public override int BaseAttackVar => 10;
    public override int BaseLifeVar => 5;

    public int BoostAttack => DynamicVars["BoostAttack"].IntValue;
    public int GraveyardDamage => DynamicVars["GraveyardDamage"].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new AttackVar("BoostAttack", 8),
        new DamageVar("GraveyardDamage", 8m, ValueProp.Unpowered)
    ];

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars["BoostAttack"].UpgradeValueBy(3m);
    }
}
