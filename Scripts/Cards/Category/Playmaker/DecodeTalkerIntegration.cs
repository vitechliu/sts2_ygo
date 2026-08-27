using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;
using VYgo.Core.Cards;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class DecodeTalkerIntegration() : BaseExtraLinkCard(-1, CardType.Skill, CardRarity.Event, TargetType.None) {
    public override int CardId => 74665150;

    public override int BaseAttackVar => 7;
    public override int BaseLifeVar => 5;
    public override int UpgradeAttackVar => 3;

    public override int GetLinkMaterialCount(CoreCard coreCard) => 2;

    public override bool CanUseLinkMaterial(SummonMaterial material) {
        return material.CoreCard.IsEffectMonster;
    }
}
