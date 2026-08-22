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
public class CyberValley() : BaseMonsterCard(1, CardRarity.Common, TargetType.None) {
    public const int GuardBlock = 5;
    public const int GuardDraw = 1;
    public const int TributeDraw = 2;

    public override int CardId => 3657444;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new BlockVar(GuardBlock, ValueProp.Move),
        new CardsVar("GuardDraw", GuardDraw),
        new CardsVar("TributeDraw", TributeDraw),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.Action(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
    ];

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 1;
    public override int UpgradeAttackVar => 1;
    public override int UpgradeLifeVar => 1;
}
