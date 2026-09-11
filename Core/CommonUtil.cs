using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;

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
    
    public static string Language => NormalizeLanguage(LocManager.Instance.Language);
    
    public static string NormalizeLanguage(string language) => language.ToLowerInvariant() switch {
        "zhs" or "zht" or "zh" or "zh_cn" or "zh_tw" or "zh-cn" or "zh-tw" => "zhs",
        "jpn" or "ja" or "ja_jp" or "ja-jp" => "jpn",
        _ => "eng"
    };
}
