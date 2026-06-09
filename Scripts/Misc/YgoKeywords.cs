using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace VYgo.Scripts.Misc;

// [RegisterOwnedCardKeyword(nameof(Unique), IconPath = "res://icon.svg", CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
// [RegisterOwnedCardKeyword(nameof(Unique2), IconPath = "res://icon.svg")] // 如果要加更多关键词，添加特性
// 由于写法和ritsulib标准不同，这里不能用static静态类！！
public class YgoKeywords {
    public static readonly CardKeyword Unique = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Unique)).GetModCardKeyword();
    // public static readonly CardKeyword Unique2 = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Unique2)).GetModCardKeyword();
}