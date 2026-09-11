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
    /// 在主菜单创建前撤销滚动功能，不改动玩家保存的 RitsuLib 设置。
    /// </summary>
    internal static void DisableMainMenuScrollingPatches() {
        try {
            string[] typeNames = [
                "MainMenuScrollCreatePatch",
                "MainMenuScrollReadyPatch",
                "MainMenuScrollFocusConnectionsPatch",
                "MainMenuScrollDefaultFocusPatch",
                "MainMenuScrollButtonInputPatch",
                "MainMenuScrollRunInfoPatch"
            ];
            var assembly = typeof(RitsuLibFramework).Assembly;
            var patchTypes = typeNames
                .Select(name => assembly.GetType($"STS2RitsuLib.Ui.MainMenu.{name}", throwOnError: false))
                .OfType<Type>()
                .ToHashSet();
            if (patchTypes.Count == 0) {
                Entry.Logger.Info("当前 RitsuLib 未提供主菜单滚动补丁，无需撤销。");
                return;
            }

            var patcher = RitsuLibFramework.CreatePatcher(Entry.ModId, "disable-ritsulib-main-menu-scrolling");
            var removedCount = 0;
            // 先取快照，撤销过程中不枚举正在改变的 Harmony 注册表。
            foreach (var original in Harmony.GetAllPatchedMethods().ToArray()) {
                var info = Harmony.GetPatchInfo(original);
                if (info == null) continue;
                var matches = info.Prefixes.Concat(info.Postfixes)
                    .Where(patch => patch.owner == SettingsUiHarmonyId
                        && patch.PatchMethod.DeclaringType is { } type && patchTypes.Contains(type))
                    .ToArray();
                foreach (var patch in matches) {
                    removedCount += patcher.UnpatchExternalPatches(
                        original, SettingsUiHarmonyId,
                        patchDeclaringType: patch.PatchMethod.DeclaringType,
                        patchMethodName: patch.PatchMethod.Name,
                        patchType: info.Prefixes.Contains(patch) ? HarmonyPatchType.Prefix : HarmonyPatchType.Postfix);
                }
            }

            // 外部撤销不会同步 RitsuLib 内部状态，以 Harmony 的实际挂载为准。
            var remainingCount = Harmony.GetAllPatchedMethods()
                .Select(Harmony.GetPatchInfo)
                .Where(info => info != null)
                .SelectMany(info => info!.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Concat(info.Finalizers))
                .Count(patch => patch.owner == SettingsUiHarmonyId
                    && patch.PatchMethod.DeclaringType is { } type && patchTypes.Contains(type));
            if (remainingCount == 0 && patchTypes.Count == typeNames.Length) {
                Entry.Logger.Info($"已禁用 RitsuLib 主菜单滚动：本次撤销 {removedCount} 个挂载，残留 0 个；玩家设置保持不变。");
            }
            else {
                Entry.Logger.Warn($"RitsuLib 主菜单滚动兼容检查不完整：找到 {patchTypes.Count}/{typeNames.Length} 个补丁类型，撤销 {removedCount} 个挂载，残留 {remainingCount} 个。");
            }
        }
        catch (Exception exception) {
            // 可选 UI 兼容失败不应阻止其他模组功能初始化。
            Entry.Logger.Warn($"禁用 RitsuLib 主菜单滚动补丁失败：{exception}");
        }
    }

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
