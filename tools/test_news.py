#!/usr/bin/env python3
"""在临时 .NET 项目验证真实新闻协议与下载缓存，不依赖游戏进程。"""
import pathlib
import subprocess
import tempfile

root = pathlib.Path(__file__).resolve().parents[1]
program = r'''
using System.Net;
using VYgo.Core.News;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

static void Check(bool value, string message) { if (!value) throw new Exception(message); }
string root = Environment.GetEnvironmentVariable("NEWS_REPO")!;
foreach (string lang in new[] {"zhs","eng","jpn"}) {
    var feed = NewsFeedCodec.Parse(File.ReadAllText($"{root}/VYgo/localization/news/{lang}.json"),lang);
    Check(feed.Items.Count == 2, "本地备用条数");
    Check(feed.Items[0].Target.Value == "news_sample", "样例场景标识");
}
string json = File.ReadAllText($"{root}/VYgo/localization/news/zhs.json");
foreach (var invalid in new[] {
    json.Replace("\"version\": 1", "\"version\": 2"),
    json.Replace("\"id\": \"workshop\"", "\"id\": \"welcome\""),
    json.Replace("res://VYgo/images/", "res://VYgo/../images/"),
    json.Replace("https://steamcommunity.com", "javascript://steamcommunity.com"),
    json.Replace("\"language\": \"zhs\"", "\"language\": \"eng\"")
}) {
    bool rejected=false; try { NewsFeedCodec.Parse(invalid,"zhs"); } catch (FormatException) {rejected=true;}
    Check(rejected,"必须拒绝非法协议");
}
Check(NewsFeedCodec.NormalizeLanguage("jpn") == "jpn", "日文语言映射");
Check(NewsFeedCodec.NormalizeLanguage("fra") == "eng", "未知语言回退");
Check(NewsFeedCodec.TryImageUri("/images/" + new string('a',64) + ".webp", out var imageUri)
    && imageUri!.AbsolutePath.StartsWith("/images/"), "图片相对 origin 解析");
int requests=0;
var handler = new Fake(async (request, token) => {
    Interlocked.Increment(ref requests);
    await Task.Yield();
    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
});
var cache = new NewsDownloadCache(new HttpClient(handler));
var first = cache.GetFeed("zhs");
Check(ReferenceEquals(first,cache.GetFeed("zhs")), "同语言复用进行中的任务");
Check((await first).Value?.Items.Count == 2, "在线成功");
Check(ReferenceEquals(first,cache.GetFeed("zhs")) && requests == 1,"完成后复用缓存");
Check((await cache.GetFeed("eng")).Error != null && requests == 2,"语言隔离并拒绝错语言数据");
var failed = new NewsDownloadCache(new HttpClient(new Fake((r,t)=>throw new HttpRequestException("offline"))));
Check((await failed.GetFeed("zhs")).Error != null,"异常转换为失败结果");
Check(ReferenceEquals(failed.GetFeed("zhs"),failed.GetFeed("zhs")),"失败不重复请求");
var empty = new NewsDownloadCache(new HttpClient(new Fake((r,t)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{\"version\":1,\"language\":\"zhs\",\"items\":[]}")}))));
Check((await empty.GetFeed("zhs")).Value?.Items.Count == 0,"空列表交由 UI 回退");
var large = new NewsDownloadCache(new HttpClient(new Fake((r,t)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(new byte[600000])}))));
Check((await large.GetFeed("zhs")).Error != null,"限制下载体积");
int imageCalls = 0;
var images = new NewsDownloadCache(new HttpClient(new Fake((r,t)=>{imageCalls++;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent([1,2,3])});})));
var imageTask = images.GetImage(new Uri("https://example.com/0.png"));
Check(ReferenceEquals(imageTask,images.GetImage(new Uri("https://example.com/0.png"))),"图片任务去重");
await imageTask;
for (int i=1;i<=NewsDownloadCache.MaxImageEntries;i++) Check((await images.GetImage(new Uri($"https://example.com/{i}.png"))).Value != null,"缓存淘汰后仍加载后续图片");
Check(imageCalls == NewsDownloadCache.MaxImageEntries+1,"图片缓存容量不会禁用后续图片");
var slow = new NewsDownloadCache(new HttpClient(new Fake(async (r,t)=>{await Task.Delay(Timeout.Infinite,t);return new HttpResponseMessage();})));
var watch=Stopwatch.StartNew();
var pending=slow.GetFeed("zhs");
Check(watch.ElapsedMilliseconds < 500,"获取任务不阻塞调用线程");
Check((await pending).Error != null && watch.Elapsed.TotalSeconds is >= 4.5 and < 7,"五秒超时");
Console.WriteLine("PASS: 三语言、协议校验、缓存、语言隔离、异常、空列表、下载上限、异步与五秒超时");
sealed class Fake(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> send) : HttpMessageHandler {
 protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken t)=>send(r,t);
}
'''
with tempfile.TemporaryDirectory(prefix='vygo-news-tests-') as folder:
    folder = pathlib.Path(folder)
    includes = ''.join(f'<Compile Include="{root / "Core/News" / name}" />' for name in ['NewsFeed.cs', 'NewsDownloadCache.cs'])
    (folder/'Tests.csproj').write_text(f'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><RollForward>Major</RollForward><NuGetAudit>false</NuGetAudit><TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><LangVersion>12</LangVersion></PropertyGroup><ItemGroup>{includes}</ItemGroup></Project>')
    (folder/'Program.cs').write_text(program)
    import os
    subprocess.run(['dotnet', 'run', '--project', str(folder/'Tests.csproj')], env={**os.environ,'NEWS_REPO':str(root)}, check=True)
