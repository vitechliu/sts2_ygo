using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace VYgo.Core.Potions;

/// <summary>
/// 按具体药水类型注册目标类型覆盖，并负责为对应的 TargetType getter 安装补丁。
/// </summary>
public static class PotionTargetModifier {
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<Type, TargetType> TargetOverrides = new();
    private static readonly HashSet<MethodInfo> PatchedGetters = new();
    private static Harmony? _harmony;

    /// <summary>
    /// 绑定用于安装具体药水 getter 补丁的 Harmony 实例。
    /// 已提前注册的覆盖会在此时一并安装。
    /// </summary>
    public static void Initialize(Harmony harmony) {
        ArgumentNullException.ThrowIfNull(harmony);

        lock (SyncRoot) {
            if (_harmony != null && _harmony.Id != harmony.Id) {
                throw new InvalidOperationException(
                    $"药水目标修改器已经绑定到 Harmony 实例 '{_harmony.Id}'，不能再次绑定到 '{harmony.Id}'。");
            }

            _harmony = harmony;
            foreach (var potionType in TargetOverrides.Keys) {
                EnsureGetterPatched(potionType);
            }
        }
    }

    /// <summary>
    /// 修改指定药水类型的投掷目标。重复注册同一类型时，以最后一次注册为准。
    /// </summary>
    public static void ModifyPotionTarget<TPotion>(TargetType targetType)
        where TPotion : PotionModel {
        ModifyPotionTarget(typeof(TPotion), targetType);
    }

    /// <summary>
    /// 移除指定药水类型的目标覆盖，使其重新使用原版 TargetType。
    /// </summary>
    public static bool RestorePotionTarget<TPotion>()
        where TPotion : PotionModel {
        lock (SyncRoot) {
            return TargetOverrides.Remove(typeof(TPotion));
        }
    }

    /// <summary>
    /// 查询指定药水类型当前注册的目标覆盖。
    /// </summary>
    public static bool TryGetModifiedTarget<TPotion>(out TargetType targetType)
        where TPotion : PotionModel {
        lock (SyncRoot) {
            return TargetOverrides.TryGetValue(typeof(TPotion), out targetType);
        }
    }

    internal static bool TryGetModifiedTarget(PotionModel potion, out TargetType targetType) {
        lock (SyncRoot) {
            return TargetOverrides.TryGetValue(potion.GetType(), out targetType);
        }
    }

    private static void ModifyPotionTarget(Type potionType, TargetType targetType) {
        if (!typeof(PotionModel).IsAssignableFrom(potionType) || potionType.IsAbstract) {
            throw new ArgumentException($"类型 '{potionType.FullName}' 必须是具体的 PotionModel。", nameof(potionType));
        }

        lock (SyncRoot) {
            TargetOverrides[potionType] = targetType;
            if (_harmony != null) {
                EnsureGetterPatched(potionType);
            }
        }
    }

    private static void EnsureGetterPatched(Type potionType) {
        var getter = AccessTools.PropertyGetter(potionType, nameof(PotionModel.TargetType));
        if (getter == null || getter.IsAbstract) {
            throw new MissingMethodException(potionType.FullName, $"get_{nameof(PotionModel.TargetType)}");
        }

        if (!PatchedGetters.Add(getter)) return;

        var postfix = AccessTools.Method(typeof(PotionTargetModifier), nameof(TargetTypePostfix));
        _harmony!.Patch(getter, postfix: new HarmonyMethod(postfix));
    }

    private static void TargetTypePostfix(PotionModel __instance, ref TargetType __result) {
        lock (SyncRoot) {
            if (TargetOverrides.TryGetValue(__instance.GetType(), out var targetType)) {
                __result = targetType;
            }
        }
    }
}
