using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
[RegisterCharacterStarterCard(typeof(PlaymakerCharacter), 1)]
public class LadyDebug() : BaseMonsterCard(1, CardType.Skill, CardRarity.Basic, TargetType.None) {
    public override int CardId => 16188701;

    public override int BaseAttackVar => 6;
    public override int BaseLifeVar => 5;

    public int ChoiceCount => DynamicVars.Cards.IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new CardsVar(3)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
    ];
}
