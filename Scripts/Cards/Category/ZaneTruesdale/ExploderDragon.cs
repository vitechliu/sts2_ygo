using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using VYgo.Core;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class ExploderDragon() : BaseMonsterCard(1, CardType.Attack, CardRarity.Common, TargetType.None) {
    public const int BaseDamage = 7;

    public override int CardId => 20586572;

    public override int BaseAttackVar => 4;
    public override int BaseLifeVar => 1;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new DamageVar(BaseDamage, ValueProp.Unpowered),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.BattleDestroyed(),
    ];

    protected override void OnUpgrade() {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
