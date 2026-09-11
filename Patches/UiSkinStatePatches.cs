using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using VYgo.Core.Settings;
using VYgo.Core.UiSkin;

namespace VYgo.Patches;

/// <summary>在原版交互结算之后更新独立状态图，不替换输入或业务方法。</summary>
[HarmonyPatch]
public static class UiSkinStatePatches {
    public static bool Prepare() => VYgoModSettings.ReplaceUiSkinOnStartup;
    public static IEnumerable<MethodBase> TargetMethods() {
        foreach (string name in new[] { "OnPressHandler", "OnReleaseHandler", "RefreshFocus", "Enable", "Disable", "OnVisibilityChanged" }) {
            var method = AccessTools.DeclaredMethod(typeof(NClickableControl), name);
            if (method != null) yield return method;
        }
        foreach (string name in new[] { "Select", "Deselect" }) {
            var method = AccessTools.DeclaredMethod(typeof(NSettingsTab), name);
            if (method != null) yield return method;
        }
        var tick = AccessTools.PropertySetter(typeof(NTickbox), nameof(NTickbox.IsTicked));
        if (tick != null) yield return tick;
        foreach (var (type, property) in new[] { (typeof(NCardPoolFilter), "IsSelected"), (typeof(NCardTypeTickbox), "IsTicked"), (typeof(NCardCostTickbox), "IsTicked"), (typeof(NCardViewSortButton), "IsDescending") }) {
            var setter = AccessTools.PropertySetter(type, property);
            if (setter != null) yield return setter;
        }
        foreach (Type type in new[] { typeof(NScrollbar), typeof(NDropdownScrollbar) }) foreach (string name in new[] { "_GuiInput", "_Input" }) {
            var method = AccessTools.DeclaredMethod(type, name);
            if (method != null) yield return method;
        }
    }
    public static void Postfix(Control __instance) => UiSkinService.Refresh(__instance);
}
