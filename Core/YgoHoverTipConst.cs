using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Scripts.Cards;

namespace VYgo.Core;

public static class YgoHoverTipConst {
    
    
    public static IHoverTip Summon(BaseMonsterCard? card = null) {
        var str = "V_YGO_SUMMON";
        var title = HoverTipFactory.L10NStatic(str + ".title");
        var description = HoverTipFactory.L10NStatic(str + ".description");
        if (card != null) {
            title.Add(card.DynamicVars["Attack"]);
            description.Add(card.DynamicVars["Attack"]);
            title.Add(card.DynamicVars["Life"]);
            description.Add(card.DynamicVars["Life"]);
        }
        return new HoverTip(title, description);
    }

    private static IHoverTip Base(string key) {
        var str = "V_YGO_" + key.ToUpper();
        var title = HoverTipFactory.L10NStatic(str + ".title");
        var description = HoverTipFactory.L10NStatic(str + ".description");
        return new HoverTip(title, description);
    }
    public static IHoverTip SummonNormal() {
        return Base("SUMMON_NORMAL");
    }
    public static IHoverTip SpecialSummon() {
        return Base("SPECIAL_SUMMON");
    }
    public static IHoverTip VoidDamage() {
        return Base("VOID_DAMAGE");
    }
    public static IHoverTip LinkSummon() {
        return Base("LINK_SUMMON");
    }
    public static IHoverTip FusionSummon() {
        return Base("FUSION_SUMMON");
    }
    //送墓
    public static IHoverTip SendToGraveyard() {
        return Base("SEND_TO_GRAVEYARD");
    }
    
    //卡名替代
    public static IHoverTip NameAs(YgoMaterialNames name) {
        var str = "V_YGO_NAME_AS";
        var title = HoverTipFactory.L10NStatic(str + ".title");
        var description = HoverTipFactory.L10NStatic(str + ".description");
        description.Add("YgoName", name.ToString());
        return new HoverTip(title, description);
    }
}