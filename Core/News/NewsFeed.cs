using System.Text.Json;
using System.Text.RegularExpressions;

namespace VYgo.Core.News;

public sealed record NewsAsset(string Type, string Value);
public sealed record NewsItem(string Id, string Title, string Content, NewsAsset Image, NewsAsset Target);
public sealed record NewsFeed(int Version, string Language, IReadOnlyList<NewsItem> Items);

/// <summary>线上与本地备用新闻共用的协议；不依赖 Godot，便于独立验证。</summary>
public static class NewsFeedCodec {
    public static readonly Uri Origin = new("https://vygo-news.i-7d5.workers.dev/");
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };
    private static readonly Regex Identifier = new("^[a-zA-Z0-9_-]{1,80}$", RegexOptions.CultureInvariant);

    public static NewsFeed Parse(string json, string language) {
        NewsFeed? feed = JsonSerializer.Deserialize<NewsFeed>(json.TrimStart('\uFEFF'), Options);
        if (feed == null || feed.Version != 1 || feed.Language != language || feed.Items == null || feed.Items.Count > 50)
            throw new FormatException("新闻版本、语言或条目数量无效。");
        HashSet<string> ids = [];
        foreach (NewsItem? item in feed.Items) {
            if (item == null || item.Id == null || !Identifier.IsMatch(item.Id) || !ids.Add(item.Id)
                || string.IsNullOrWhiteSpace(item.Title) || item.Title.Length > 200
                || item.Content == null || item.Content.Length > 4000 || item.Image == null || item.Target == null)
                throw new FormatException("新闻条目字段无效或 ID 重复。");
            if (item.Image.Type == "res") {
                if (!IsResourceImage(item.Image.Value)) throw new FormatException("新闻资源图片路径无效。");
            }
            else if (item.Image.Type != "url" || !TryImageUri(item.Image.Value, out _))
                throw new FormatException("新闻在线图片地址无效。");
            if (item.Target.Value == null || item.Target.Type switch {
                    "none" => false,
                    "url" => !IsHttps(item.Target.Value),
                    "scene" => !Identifier.IsMatch(item.Target.Value),
                    _ => true
                }) throw new FormatException("新闻跳转目标无效。");
        }
        return feed;
    }

    public static bool IsHttps(string? value) => Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
        && uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo);

    public static bool IsResourceImage(string? value) => value != null
        && Regex.IsMatch(value, @"^res://VYgo/[a-zA-Z0-9_./-]+\.(png|jpg|jpeg|webp)$")
        && !value.Contains("..");

    public static bool TryImageUri(string? value, out Uri? uri) {
        uri = null;
        if (value != null && Regex.IsMatch(value, @"^/images/[a-f0-9]{64}\.webp$")) {
            uri = new Uri(Origin, value);
            return true;
        }
        if (!IsHttps(value)) return false;
        uri = new Uri(value!);
        return true;
    }
}
