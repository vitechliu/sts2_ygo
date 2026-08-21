using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Ui.Toast;
using VYgo.Core.History;
using VYgo.Scripts.Cards;

namespace VYgo.Core;

//控制效果发动次数
public static class CommonUtil {
    //从卡组顶堆墓
    public static async Task<bool> SendToGraveyardFromDeck(
        Player player,
        int count) {
        var drawPile = PileType.Draw.GetPile(player);
        if (drawPile.Cards.Count < count) return false;
        var cardModels = drawPile.Cards.Take(count).ToList();
        if (cardModels.Count < count) return false;
        await CardPileCmd.Add(cardModels, PileType.Discard);
        return true;
    }
}
