using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberPhoenix() : BaseMonsterCard(1, CardRarity.Common, TargetType.None) {
    public const int BaseDraw = 1;
    private const int UpgradeDraw = 1;

    public override int CardId => 3370104;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new CardsVar(BaseDraw),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.SendToGraveyard(),
    ];

    public override int BaseAttackVar => 2;
    public override int BaseLifeVar => 3;

    protected override void OnUpgrade() {
        DynamicVars.Cards.UpgradeValueBy(UpgradeDraw);
    }
}
