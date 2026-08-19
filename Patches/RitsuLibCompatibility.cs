using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using STS2RitsuLib;
using STS2RitsuLib.Patching.Models;
using VYgo.Scripts;

namespace VYgo.Patches;

/// <summary>
/// 对 RitsuLib 的可选兼容调整。
/// </summary>
internal static class RitsuLibCompatibility {
    private const string SettingsUiHarmonyId =
        STS2RitsuLib.Const.ModId + ".framework-settings-ui";

    private const string MainMenuSettingsPatchTypeName =
        "STS2RitsuLib.Settings.Patches.MainMenuModSettingsButtonPatch";

    /// <summary>
    /// 禁用 RitsuLib 注入原版主菜单设置快捷入口的两个 Postfix。
    /// </summary>
    internal static void DisableMainMenuSettingsButtonPatch() {
        try {
            var patchType = typeof(RitsuLibFramework).Assembly.GetType(
                MainMenuSettingsPatchTypeName,
                throwOnError: false);
            if (patchType == null) {
                Entry.Logger.Warn($"未找到 RitsuLib 主菜单设置按钮 Patch 类型：{MainMenuSettingsPatchTypeName}");
                return;
            }

            var patcher = RitsuLibFramework.CreatePatcher(
                Entry.ModId,
                "disable-ritsulib-main-menu-settings");
            var removedCount = 0;

            foreach (var target in new[] {
                         new ModPatchTarget(typeof(NMainMenu), nameof(NMainMenu._Ready)),
                         new ModPatchTarget(typeof(NMainMenu), "OnSubmenuStackChanged"),
                     }) {
                removedCount += patcher.UnpatchExternalPatches(
                    target,
                    SettingsUiHarmonyId,
                    patchDeclaringType: patchType,
                    patchMethodName: "Postfix",
                    patchType: HarmonyPatchType.Postfix,
                    ignoreIfTargetMissing: true);
            }

            if (removedCount == 2) {
                Entry.Logger.Info("已禁用 RitsuLib 主菜单设置按钮 Patch。");
            }
            else {
                Entry.Logger.Warn($"RitsuLib 主菜单设置按钮预期卸载 2 个 Postfix，实际卸载 {removedCount} 个。");
            }
        }
        catch (Exception exception) {
            // 第三方 UI 兼容调整失败不应阻止 VYgo 加载。
            Entry.Logger.Warn($"禁用 RitsuLib 主菜单设置按钮 Patch 失败：{exception.Message}");
        }
    }
}
