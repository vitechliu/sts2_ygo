using System.Text.Json;
using VYgo.Scripts;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using FileAccess = Godot.FileAccess;

namespace VYgo.Core.News;

internal static class NewsLocalData {
    public static string Language => CommonUtil.NormalizeLanguage(LocManager.Instance.Language);

    public static IReadOnlyList<NewsItem> LoadFeed(string language) {
        try {
            return NewsFeedCodec.Parse(FileAccess.GetFileAsString($"res://VYgo/localization/news/{language}.json"), language).Items;
        }
        catch (Exception e) {
            Entry.Logger.Warn($"读取本地新闻失败：{e.Message}");
            return language == "eng" ? [] : LoadFeed("eng");
        }
    }

    public static Dictionary<string, string> LoadPageTexts() {
        string path = $"res://VYgo/localization/{Language}/main_menu.json";
        try { return JsonSerializer.Deserialize<Dictionary<string, string>>(FileAccess.GetFileAsString(path).TrimStart('\uFEFF')) ?? []; }
        catch (Exception e) { Entry.Logger.Warn($"读取公告页面文案失败：{e.Message}"); return []; }
    }
}
