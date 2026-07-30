using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using VYgo.Scripts;

namespace VYgo.Core;

[RegisterOwnedCardKeyword(nameof(Piercing), IconPath = null, CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.AfterCardDescription)]
[RegisterOwnedCardKeyword(nameof(GroupAttack), IconPath = null, CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.AfterCardDescription)]
public class YgoKeywords {
    public static readonly CardKeyword Piercing = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Piercing)).GetModCardKeyword();
    public static readonly CardKeyword GroupAttack = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(GroupAttack)).GetModCardKeyword();
}
