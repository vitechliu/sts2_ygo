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
        var str = "V_YGO_" + key.ToUpperInvariant();
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
    public static IHoverTip ContactFusion() {
        return Base("CONTACT_FUSION");
    }
    public static IHoverTip XyzSummon() {
        return Base("XYZ_SUMMON");
    }
    public static IHoverTip SynchroSummon() {
        return Base("SYNCHRO_SUMMON");
    }
    //送墓
    public static IHoverTip SendToGraveyard() {
        return Base("SEND_TO_GRAVEYARD");
    }

    public static IHoverTip BattleDestroyed() {
        return Base("BATTLE_DESTROYED");
    }

    //登场
    public static IHoverTip EnterField() {
        return Base("ENTER_FIELD");
    }

    public static IHoverTip Equip() {
        return Base("EQUIP");
    }

    public static IHoverTip Enhance() {
        return Base("ENHANCE");
    }

    public static IHoverTip SetCard() {
        return Base("SET_CARD");
    }
    
    //卡牌启动
    public static IHoverTip Action() {
        return Base("ACTION");
    }
    //能力启动
    public static IHoverTip PowerAction() {
        return Base("POWER_ACTION");
    }
    
    //手发启动
    public static IHoverTip HandAction() {
        return Base("HAND_ACTION");
    }
    
    //超量素材
    public static IHoverTip XYZMaterial() {
        return Base("XYZ_MATERIAL");
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
