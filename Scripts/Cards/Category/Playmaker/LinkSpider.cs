using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
[RegisterCharacterStarterCard(typeof(PlaymakerCharacter), 1)]
public class LinkSpider() : BaseExtraLinkCard(-1, CardRarity.Basic, TargetType.None) {
    public override int CardId => 98978921;

    public override int BaseAttackVar => 3;
    public override int BaseLifeVar => 1;
    public override int UpgradeAttackVar => 2;

    public override bool CanUseLinkMaterial(SummonMaterial material) {
        return !material.CoreCard.IsEffectMonster;
    }
}
