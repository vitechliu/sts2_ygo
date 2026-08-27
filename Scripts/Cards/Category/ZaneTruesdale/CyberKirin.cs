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
public class CyberKirin() : BaseMonsterCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.None) {
    public const int IntangibleAmount = 1;

    public override int CardId => 76986005;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new PowerVar<IntangiblePower>(IntangibleAmount),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.Action(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromPower<IntangiblePower>(),
    ];

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 5;
    public override int UpgradeAttackVar => 1;
    public override int UpgradeLifeVar => 1;
}
