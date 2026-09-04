# 日文名称查询与证据

查证日期：2026-09-04。API 可能变化，先用一个已知真实卡号确认字段，再开始批量任务。游戏目录用 `jpn`，外部服务可能用 `jp_name` 或 `ja`，二者不可机械替换。

## 首选：百鸽 API

百鸽是社区维护的数据聚合服务，不是 Konami 官方 API，也不是“官方维基”。[服务自己的接口文档](https://ygocdb.com/api) 将日文名称字段定义为官方日文名，并说明 v0 结构不稳定。

```text
GET https://ygocdb.com/api/v0/card/{card_id}
id == 请求的真实卡片密码
cid = Konami 数据库编号
text.jp_name = 要采用的日文名称
```

缺少 `text.jp_name` 表示未提供名称，不用中文、读音或英文兜底。`show=all`、搜索与 ZIP 接口的结构不同，不要把不同端点的字段路径混用。

脚本使用 Python 3 标准库，按传入顺序去重、串行请求、检查响应 id、缓存有来源的成功结果。HTTP/网络错误、空字段和不匹配的 id 会进入 `unresolved`，进程退出 1；成功项仍输出并缓存。没有自动重试风暴，也不写入游戏文件。

在项目根目录运行（先由 C# / 数据确认这些是实际卡号）：

```bash
python3 .agents/skills/design-localization/scripts/fetch-japanese-names.py \
  70095154 29301450 --cache /tmp/vygo-japanese-names.json
```

`--offline` 仅读缓存；`--refresh` 强制重新查证指定卡。默认缓存无自动过期时间，输出保留原始查询时间；复查来源、版本迁移或用户要求更新时用 `--refresh`。错误缓存不作为有效证据。

较大批量可改用文档的 `POST /api/v0/cardset`，请求体 `{"ids":[...]}`，每批最多 100 项，只发送已核实的真实卡号。这是读取查询；响应按请求号索引，每项仍要验证 `id` 与 `text.jp_name`，缺失键不代表成功。通常只查询本项目需要的卡，不下载全站；如确需使用 ZIP，遵循文档的 MD5 更新检查。

## 权威核对：Konami 日文数据库

官方页面（HTML 页面，不称其为公开 JSON API）：

```text
https://www.db.yugioh-card.com/yugiohdb/card_search.action?ope=2&cid={cid}&request_locale=ja
```

`cid` 来自已验证 API 返回，不能用 `card_id` 或 `db.json` 的行号替代。出现异名、特殊标点、衍生物、API 字段缺失或来源冲突时读取该页；记录可见卡名或来源卡效果中实际出现的衍生物名。官方冲突修订需同步所有同号名称和证据记录。

## 备用：YGOResources

[该站 API 文档](https://db.ygoresources.com/about/api) 说明其所有编号是 Konami 数据库编号。它也是社区服务。

```text
GET https://db.ygoresources.com/data/card/{cid}
cardData.ja.name
```

调用前必须已有可靠 `cid` 映射，并核对响应 `cardId`、`cardData.ja.id`；语言可能缺失。缓存响应及 `X-Cache-Revision`，按其文档使用 `/manifest/{revision}` 识别变化；不要请求整个数据库。不在主脚本里隐式自动回退，以便暴露来源变化与身份问题。

没有可靠 cid 时，通过官方站点检索名称并核对身份；可以继续寻找提供者自己的 API 文档，但不能臆造 Yugipedia/其他 Wiki 的端点或把搜索摘要当作已读取的名称证据。

## 本次实测

| 本地真实 CardId | 百鸽 cid | `text.jp_name` | 核对 |
| --- | --- | --- | --- |
| 70095154 | 6390 | サイバー・ドラゴン | 单卡 API 成功，Konami 日文页面同名。 |
| 29301450 | 19188 | S：Pリトルナイト | 单卡 API 成功，保留全角冒号。 |

卡号只是示例，实际任务从当前类与数据建立映射。缓存是查询证据，不会证明某个本地 key 一定对应这张卡；尤其不能把自定义衍生物的“来源号 + 1”当作官方密码。
