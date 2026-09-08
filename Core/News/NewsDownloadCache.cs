using System.Net.Http;
using System.Threading;

namespace VYgo.Core.News;

public sealed record NewsDownload<T>(T? Value, string? Error) where T : class;

/// <summary>
/// 进程内按语言只请求一次（含失败）；下载与解析均在后台，不捕获 Godot 节点。
/// UI 在自己的帧回调中消费完成结果，离开主菜单后仍可完成缓存。
/// </summary>
public sealed class NewsDownloadCache {
    public static readonly NewsDownloadCache Shared = new(new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }));
    internal NewsDownloadCache(HttpClient client) { Client = client; }
    private readonly HttpClient Client;
    private readonly Dictionary<string, Task<NewsDownload<NewsFeed>>> Feeds = [];
    private readonly Dictionary<string, Task<NewsDownload<byte[]>>> Images = [];
    private readonly SemaphoreSlim ImageSlots = new(4);
    private readonly object Gate = new();
    public const int MaxImageEntries = 32;

    public Task<NewsDownload<NewsFeed>> GetFeed(string language) {
        language = NewsFeedCodec.NormalizeLanguage(language);
        lock (Gate) {
            if (Feeds.TryGetValue(language, out var cached)) return cached;
            return Feeds[language] = Task.Run(async () => {
                try {
                    byte[] bytes = await Download(new Uri(NewsFeedCodec.Origin, $"news/{language}.json"), 512 * 1024);
                    return new NewsDownload<NewsFeed>(NewsFeedCodec.Parse(System.Text.Encoding.UTF8.GetString(bytes), language), null);
                }
                catch (Exception e) { return new NewsDownload<NewsFeed>(null, e.Message); }
            });
        }
    }

    public Task<NewsDownload<byte[]>> GetImage(Uri uri) {
        lock (Gate) {
            string key = uri.AbsoluteUri;
            if (Images.TryGetValue(key, out var cached)) return cached;
            if (Images.Count >= MaxImageEntries) {
                string? completedKey = Images.FirstOrDefault(pair => pair.Value.IsCompleted).Key;
                if (completedKey != null) Images.Remove(completedKey);
                else return Task.FromResult(new NewsDownload<byte[]>(null, "新闻图片下载队列已满。"));
            }
            return Images[key] = Task.Run(async () => {
                try { return new NewsDownload<byte[]>(await Download(uri, 4 * 1024 * 1024, ImageSlots), null); }
                catch (Exception e) { return new NewsDownload<byte[]>(null, e.Message); }
            });
        }
    }

    private async Task<byte[]> Download(Uri uri, int limit, SemaphoreSlim? slots = null) {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        bool acquired = false;
        try {
            if (slots != null) { await slots.WaitAsync(timeout.Token).ConfigureAwait(false); acquired = true; }
            using var response = await Client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > limit) throw new FormatException("新闻下载内容超过大小限制。");
            await using var input = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            using var output = new MemoryStream();
            byte[] buffer = new byte[16384];
            int count;
            while ((count = await input.ReadAsync(buffer, timeout.Token).ConfigureAwait(false)) > 0) {
                if (output.Length + count > limit) throw new FormatException("新闻下载内容超过大小限制。");
                output.Write(buffer, 0, count);
            }
            timeout.Token.ThrowIfCancellationRequested();
            return output.ToArray();
        }
        finally { if (acquired) slots!.Release(); }
    }
}
