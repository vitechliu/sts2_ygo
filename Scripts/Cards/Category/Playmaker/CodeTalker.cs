using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
[RegisterCharacterStarterCard(typeof(PlaymakerCharacter), 1)]
public class CodeTalker() : BaseExtraLinkCard(-1, CardRarity.Basic, TargetType.None) {
    public override int CardId => 53413628;

    public override int BaseAttackVar => 3;
    public override int BaseLifeVar => 1;
    public override int UpgradeAttackVar => 2;

    public int BoostAttack => DynamicVars["BoostAttack"].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new AttackVar("BoostAttack", 5)
    ];

    public override bool CanUseLinkMaterial(SummonMaterial material) {
        return material.CoreCard.IsEffectMonster;
    }
}
