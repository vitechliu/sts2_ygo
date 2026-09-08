# 主菜单在线新闻

生产入口：`https://vygo-news.i-7d5.workers.dev/news/{zhs|eng|jpn}.json`。
协议与 `External/vygo-news` 内容编辑器一致，版本为 1。

## 生命周期

- 首次进入主菜单立即显示 `VYgo/localization/news/{language}.json`，后台获取在线列表。
- 每个游戏进程、每种语言最多尝试一次（失败也不重试）；返回主菜单复用任务和结果。
- 切换语言使用独立缓存，旧语言结果不会覆盖新语言。其他语言回退英文，繁体中文使用简体备用内容。
- 网络响应和正文读取共用五秒取消令牌；非成功状态、重定向、格式错误、空列表均保留本地内容。
- UI 仅在帧回调消费结果；任务不捕获节点，菜单释放后不会回调已释放的 UI。
- JSON 上限 512 KiB / 50 条；图片单次下载上限 4 MiB、超时五秒。图片失败不影响新闻文字与跳转。
- PNG、JPEG、WebP 在当前帧解码为纹理；大图缩至 1600×1200 范围，拒绝宽高超过 4096 的图片。
- 图片下载并发上限 4，下载和纹理缓存各保留最多 32 条，满后淘汰旧条目。重启游戏可重新请求。

## 图片和跳转

- `image.type=res`：只加载 `res://VYgo/` 下的图片。
- `image.type=url`：HTTPS 图片，或编辑器生成的 `/images/<sha256>.webp`；相对路径以内容站点 origin 解析。
- `target.type=none`：不可点击。
- `target.type=url`：由 `PlatformUtil.OpenUrl` 打开 HTTPS 链接；Steam 叠加界面不可用时沿用原版系统浏览器回退。
- `target.type=scene`：只识别 `NewsSceneRouter` 注册表中的标识。当前样例 `news_sample`，未知标识保持新闻可见但禁用点击。
- 样例场景复用游戏 `patch_screen_contents.tscn` 和 `back_button.tscn`，通过 `NSubmenuStack.Push` 打开。后续增加页面时在注册表加入其场景路径，并确保根节点继承 `NSubmenu`。
- 本地新闻按普通文本展示；样例公告正文的受信任本地文案使用 BBCode，位于三语言 `main_menu.json` 的 `NEWS_SAMPLE_*`。

## 验证

运行 `python3 tools/test_news.py`：校验真实协议和下载服务的三语言、缓存复用、失败、大小限制、图片淘汰及五秒超时。测试程序集以 net9.0 编译，允许使用更新主版本的已安装运行时。

场景、图片或本地文案改变后运行 `./publish.sh`。构建与 PCK 打包不等于游戏 UI 验收；需在游戏中验证：鼠标/键盘/手柄点击、箭头不触发跳转、原生返回、单条新闻导航、切换语言、断网回退、在线图片失败、离开主菜单时下载完成。
