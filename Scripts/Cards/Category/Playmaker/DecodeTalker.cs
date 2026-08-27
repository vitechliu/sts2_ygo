using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core;
using VYgo.Core.Cards;
using VYgo.Scripts.Powers;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
[RegisterCharacterStarterCard(typeof(PlaymakerCharacter), 1)]
public class DecodeTalker() : BaseExtraLinkCard(-1, CardType.Attack, CardRarity.Basic, TargetType.None) {
    public override int CardId => 1861629;

    public override int BaseAttackVar => 7;
    public override int BaseLifeVar => 5;
    public override int UpgradeAttackVar => 3;

    public int BoostAttack => DynamicVars["BoostAttack"].IntValue;
    public int Negating => DynamicVars["NegatingPower"].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new AttackVar("BoostAttack", 5),
        new PowerVar<NegatingPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.LinkSummon(),
        HoverTipFactory.FromPower<NegatingPower>()
    ];

    public override int GetLinkMaterialCount(CoreCard coreCard) => 2;

    public override bool CanUseLinkMaterial(SummonMaterial material) {
        return material.CoreCard.IsEffectMonster;
    }
}
