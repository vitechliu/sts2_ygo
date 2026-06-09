using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;

namespace VYgo.Scripts;

// 必须要加的属性，用于注册Mod。字符串和初始化函数命名一致。
[ModInitializer(nameof(Initialize))]
public static class Entry {
    public const string ModId = "VYgo";
    public const string ResPath = $"res://{ModId}";

    public static Logger Logger { get; private set; } = null!;

    public static void Initialize() {
        var assembly = Assembly.GetExecutingAssembly();
        Logger = RitsuLibFramework.CreateLogger(ModId);
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
        Logger.Info("VYgo initialized.");
    }
}