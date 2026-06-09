using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Placeholders;

[RegisterCard(typeof(RedhatCardPool))]
[RegisterCharacterStarterCard(typeof(RedhatCharacter), 5)]
public class AttackBasic(): BasePlaceholder(CardType.Attack, CardRarity.Basic) {
}