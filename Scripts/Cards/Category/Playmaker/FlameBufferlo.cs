using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class FlameBufferlo() : BaseMonsterCard(1, CardRarity.Common, TargetType.None) {
    public override int CardId => 80794697;

    public override int BaseAttackVar => 5;
    public override int BaseLifeVar => 1;

    public int Draw => DynamicVars.Cards.IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new CardsVar(2)
    ];

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
