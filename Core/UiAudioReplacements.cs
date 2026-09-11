using System.Text.Json;
using STS2RitsuLib;
using STS2RitsuLib.Audio;
using VYgo.Scripts;

namespace VYgo.Core;

/// <summary>读取后台导出的事件映射，按原版事件统一替换所有 UI 调用。</summary>
public static class UiAudioReplacements {
    private const string Root = "res://VYgo/";
    private static readonly Dictionary<string, string> Active = new(StringComparer.Ordinal);
    private static IDisposable? _readySubscription;

    public static void Initialize() {
        try {
            using var catalogStream = typeof(UiAudioReplacements).Assembly.GetManifestResourceStream("VYgo.Audio.Catalog")!;
            using var catalog = JsonDocument.Parse(catalogStream);
            var allowed = catalog.RootElement.EnumerateArray()
                .Select(item => item.GetProperty("path").GetString()!).ToHashSet(StringComparer.Ordinal);
            using var mappingStream = typeof(UiAudioReplacements).Assembly.GetManifestResourceStream("VYgo.Audio.Replacements")!;
            using var document = JsonDocument.Parse(mappingStream);
            var data = document.RootElement;
            if (data.GetProperty("version").GetInt32() != 1) throw new InvalidDataException("不支持的音效映射版本。");
            var profiles = data.GetProperty("profiles").EnumerateArray()
                .ToDictionary(item => item.GetProperty("id").GetString()!, item => item.Clone());
            var pending = new Dictionary<string, (string Path, string Guid)>(StringComparer.Ordinal);
            var banks = new HashSet<string>(StringComparer.Ordinal);
            var guids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var mapping in data.GetProperty("mappings").EnumerateObject()) {
                if (!allowed.Contains(mapping.Name)) continue;
                var value = mapping.Value;
                if (!profiles.TryGetValue(value.GetProperty("profileId").GetString()!, out var profile)) continue;
                var target = value.GetProperty("event").GetString()!;
                if (allowed.Contains(target) || target.Contains("/music/", StringComparison.OrdinalIgnoreCase)
                    || target.Contains("/bgm/", StringComparison.OrdinalIgnoreCase)) continue;
                var targetEvent = profile.GetProperty("events").EnumerateArray()
                    .FirstOrDefault(item => item.GetProperty("path").GetString() == target);
                if (targetEvent.ValueKind == JsonValueKind.Undefined) continue;
                var eventGuid = targetEvent.GetProperty("guid").GetString()!;
                if (!Guid.TryParse(eventGuid, out _)) continue;
                foreach (var bank in profile.GetProperty("banks").EnumerateArray()) banks.Add(ResourcePath(bank.GetString()!));
                guids.Add(ResourcePath(profile.GetProperty("guidFile").GetString()!));
                pending[mapping.Name] = (target, eventGuid);
            }
            foreach (var bank in banks) FmodStudioDeferredBankRegistration.RegisterBank(bank);
            foreach (var guid in guids) FmodStudioDeferredBankRegistration.RegisterStudioGuidMappings(guid);
            _readySubscription ??= RitsuLibFramework.SubscribeLifecycleOnce<DeferredInitializationCompletedEvent>(_ => {
                foreach (var (source, target) in pending) {
                    // 检查实际已加载的 GUID，不能只凭路径表存在就替换，以免丢失原版声音。
                    if (FmodStudioServer.TryCheckEventGuid(target.Guid) == true) Active[source] = target.Path;
                    else Entry.Logger.Warn($"UI 音效尚未加载，保留原版：{source} -> {target.Path}");
                }
                Entry.Logger.Info($"已启用 {Active.Count} 项 UI 音效替换。");
            }, replayCurrentState: true);
        } catch (Exception error) {
            Active.Clear();
            Entry.Logger.Warn($"UI 音效配置加载失败，保留原版：{error.Message}");
        }
    }

    private static string ResourcePath(string path) {
        if (!path.StartsWith("banks/", StringComparison.Ordinal) || path.Contains("..") || path.Contains('\\') || path.Contains(':'))
            throw new InvalidDataException("音效资源必须位于 Mod 的 banks 目录。");
        return Root + path;
    }

    public static void Replace(ref string path) {
        if (Active.TryGetValue(path, out var target)) path = target;
    }
}
