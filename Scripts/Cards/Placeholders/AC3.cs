using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Placeholders;

[RegisterCard(typeof(RedhatCardPool))]
public class AC3(): BasePlaceholder(CardType.Attack, CardRarity.Common) {
}