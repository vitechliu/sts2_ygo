using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;
using VYgo.Core.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class DecodeTalkerExtended() : BaseExtraLinkCard(-1, CardType.Attack, CardRarity.Event, TargetType.None) {
    public override int CardId => 30822527;

    public override int BaseAttackVar => 7;
    public override int BaseLifeVar => 5;
    public override int UpgradeAttackVar => 3;

    public int BoostAttack => DynamicVars["BoostAttack"].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new AttackVar("BoostAttack", 5)
    ];

    public override int GetLinkMaterialCount(CoreCard coreCard) => 2;

    public override bool CanUseLinkMaterial(SummonMaterial material) {
        return material.CoreCard.IsEffectMonster;
    }
}
