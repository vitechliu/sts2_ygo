using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;
using VYgo.Scripts;

namespace VYgo.Core.Settings;

/// <summary>
/// VYgo 的全局设置数据。后续增加真实设置时，可继续在此模型中添加字段。
/// </summary>
public sealed class VYgoSettingsData {
    public bool PlaceholderToggle { get; set; }
    public int PlaceholderValue { get; set; } = 50;

    public EffectMode EffectAnimationMode { get; set; } = EffectMode.full;
}

/// <summary>
/// 召唤特效动画完整度
/// </summary>
public enum EffectMode {
    none = 0, // 完全无动画
    full = 1, // 完整动画
    minimal = 2 // 快速动画
}

/// <summary>
/// 注册 VYgo 的持久化设置数据与 RitsuLib 设置页面。
/// </summary>
public static class VYgoModSettings {
    private const string DataKey = "settings";
    private const string FileName = "settings.json";

    /// <summary>
    /// 获取指定玩家在本机应使用的召唤动画模式。
    /// 联机中的远端玩家始终不播放召唤演出，本地玩家使用自己的全局设置。
    /// </summary>
    public static EffectMode GetEffectMode(Player effectPlayer) {
        ArgumentNullException.ThrowIfNull(effectPlayer);

        if (LocalContext.NetId.HasValue && !LocalContext.IsMe(effectPlayer)) {
            return EffectMode.none;
        }

        return RitsuLibFramework.GetDataStore(Entry.ModId)
            .Get<VYgoSettingsData>(DataKey)
            .EffectAnimationMode;
    }

    /// <summary>
    /// 在 <see cref="RitsuLibFramework.BeginModDataRegistration"/> 作用域内注册全局设置数据。
    /// </summary>
    public static void RegisterData() {
        RitsuLibFramework.GetDataStore(Entry.ModId).Register(
            key: DataKey,
            fileName: FileName,
            scope: SaveScope.Global,
            defaultFactory: static () => new VYgoSettingsData(),
            autoCreateIfMissing: true);
    }

    /// <summary>
    /// 在设置数据注册完成后注册设置页面。
    /// </summary>
    public static void RegisterPage() {
        var effectAnimationModeBinding = new ModSettingsValueBinding<VYgoSettingsData, EffectMode>(
            Entry.ModId,
            DataKey,
            SaveScope.Global,
            settings => settings.EffectAnimationMode,
            (settings, value) => settings.EffectAnimationMode = value);
        
        var placeholderToggleBinding = new ModSettingsValueBinding<VYgoSettingsData, bool>(
            Entry.ModId,
            DataKey,
            SaveScope.Global,
            settings => settings.PlaceholderToggle,
            (settings, value) => settings.PlaceholderToggle = value);

        var placeholderValueBinding = new ModSettingsValueBinding<VYgoSettingsData, int>(
            Entry.ModId,
            DataKey,
            SaveScope.Global,
            settings => settings.PlaceholderValue,
            (settings, value) => settings.PlaceholderValue = value);

        RitsuLibFramework.RegisterModSettings(Entry.ModId, page => page
            .WithTitle(ModSettingsText.Literal("VYgo 设置"))
            .WithModDisplayName(ModSettingsText.Literal("VYgo"))
            .WithDescription(ModSettingsText.Literal("杀戮尖塔 2 YGO Mod 的基础设置页面。"))
            .AddSection("general", section => section
                .WithTitle(ModSettingsText.Literal("通用"))
                .AddChoice(
                    "effect_animation_mode",
                    ModSettingsText.Literal("召唤动画复杂度"),
                    effectAnimationModeBinding,
                    [
                        new ModSettingsChoiceOption<EffectMode>(
                            EffectMode.full,
                            ModSettingsText.Literal("完整动画")
                        ),
                        new ModSettingsChoiceOption<EffectMode>(
                            EffectMode.minimal,
                            ModSettingsText.Literal("快速动画")
                        ),
                        new ModSettingsChoiceOption<EffectMode>(
                            EffectMode.none,
                            ModSettingsText.Literal("无动画")
                        )
                    ],
                    ModSettingsText.Literal("完整动画保留全部召唤演出；快速动画只保留素材闪光与结果卡飞出；无动画会跳过所有召唤演出。"),
                    ModSettingsChoicePresentation.Dropdown)
                // .AddInfoCard(
                //     "placeholder_notice",
                //     ModSettingsText.Literal("设置占位区"),
                //     ModSettingsText.Literal("以下选项用于预留设置结构，当前不会影响游戏玩法。"))
                .AddToggle(
                    "placeholder_toggle",
                    ModSettingsText.Literal("占位开关"),
                    placeholderToggleBinding,
                    ModSettingsText.Literal("预留的布尔设置项，当前没有实际效果。"))
                .AddIntSlider(
                    "placeholder_value",
                    ModSettingsText.Literal("占位数值"),
                    placeholderValueBinding,
                    minValue: 0,
                    maxValue: 100,
                    description: ModSettingsText.Literal("预留的整数设置项，当前没有实际效果。"))));
    }
}
