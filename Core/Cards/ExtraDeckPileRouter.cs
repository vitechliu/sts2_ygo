using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using VYgo.Scripts;
using VYgo.Scripts.Cards;

namespace VYgo.Core.Cards;

/// <summary>
/// 统一维护额外卡组怪兽的牌堆不变量。
/// 额外卡组怪兽可以存在于永久卡组，但在战斗中不能进入手牌或抽牌堆。
/// </summary>
public static class ExtraDeckPileRouter {
    public static bool ShouldRedirect(CardModel card, CardPile destination) {
        return card is BaseExtraCard
               && destination.Type is PileType.Hand or PileType.Draw;
    }

    public static CardPile Resolve(CardModel card, CardPile destination) {
        return ShouldRedirect(card, destination)
            ? Entry.ExtraPile.GetPile(card.Owner)
            : destination;
    }
}
