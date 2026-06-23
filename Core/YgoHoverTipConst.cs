using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Scripts.Cards;

namespace VYgo.Core;

public static class YgoHoverTipConst {
    public static IHoverTip Summon(BaseMonsterCard card) {
        var str = "V_YGO_SUMMON";
        var title = HoverTipFactory.L10NStatic(str + ".title");
        var description = HoverTipFactory.L10NStatic(str + ".description");
        title.Add(card.DynamicVars["Attack"]);
        description.Add(card.DynamicVars["Attack"]);
        title.Add(card.DynamicVars["Life"]);
        description.Add(card.DynamicVars["Life"]);
        return new HoverTip(title, description);
    }
}