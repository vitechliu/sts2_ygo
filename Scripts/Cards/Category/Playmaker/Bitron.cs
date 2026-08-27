using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
[RegisterCharacterStarterCard(typeof(PlaymakerCharacter), 2)]
public class Bitron() : BaseMonsterCard(1, CardType.Skill, CardRarity.Basic, TargetType.None) {
    public override int CardId => 36211150;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 4;
    public override int UpgradeLifeVar => 1;
}
