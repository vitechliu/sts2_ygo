using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core;
using VYgo.Core.Cards;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
[RegisterCharacterStarterCard(typeof(PlaymakerCharacter), 1)]
public class FirewallDragon() : BaseExtraLinkCard(-1, CardRarity.Basic, TargetType.None) {
    public override int CardId => 5043010;

    public override int BaseAttackVar => 10;
    public override int BaseLifeVar => 10;

    public int RecycleCount => DynamicVars.Cards.IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new CardsVar(1)
    ];

    public override int GetLinkMaterialCount(CoreCard coreCard) => 2;

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
