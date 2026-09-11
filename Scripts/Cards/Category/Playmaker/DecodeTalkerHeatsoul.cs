using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core.Cards;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class DecodeTalkerHeatsoul() : BaseExtraLinkCard(-1, CardType.Attack, CardRarity.Event, TargetType.None) {
    public override int CardId => 61245672;

    public override int BaseAttackVar => 15;
    public override int BaseLifeVar => 10;
    public override int UpgradeAttackVar => 5;

    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Concat([new CardsVar(1)]);

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars.Cards.UpgradeValueBy(1);
    }

    public override int GetLinkMaterialCount(CoreCard coreCard) => 2;

    public override int? GetMaxLinkMaterialCount(CoreCard coreCard) => 2;

    public override bool CanUseLinkMaterial(SummonMaterial material) {
        return material.CoreCard.IsEffectMonster;
    }
}
