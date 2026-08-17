using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
[RegisterCharacterStarterCard(typeof(PlaymakerCharacter), 2)]
public class Digitron() : BaseMonsterCard(1, CardRarity.Basic, TargetType.None) {
    public override int CardId => 32295838;

    public override int BaseAttackVar => 4;
    public override int BaseLifeVar => 1;
    public override int UpgradeAttackVar => 2;
}
