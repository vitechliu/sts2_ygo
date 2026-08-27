using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class Latency() : BaseMonsterCard(1, CardType.Skill, CardRarity.Common, TargetType.None) {
    public override int CardId => 3560069;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 1;

    public int Draw => DynamicVars.Cards.IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new CardsVar(1)
    ];

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
