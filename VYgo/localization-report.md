# 英日文本地化交付记录

日期：2026-09-04。基准为本次工作时的 `localization/zhs/`，未修改中文源表或卡牌效果代码。

## 覆盖范围

| 文件 | 中文键数 | eng | jpn |
| --- | ---: | ---: | ---: |
| cards.json | 368 | 368 | 368 |
| monsters.json | 146 | 146 | 146 |
| powers.json | 137 | 137 | 137 |
| characters.json | 45 | 45 | 45 |
| static_hover_tips.json | 45 | 45 | 45 |
| events.json | 42 | 42 | 42 |
| relics.json | 10 | 10 | 10 |
| main_menu.json | 7 | 7 | 7 |
| combat_messages.json | 7 | 7 | 7 |
| card_keywords.json | 4 | 4 | 4 |
| card_library.json | 1 | 1 | 1 |
| **总计** | **812** | **812** | **812** |

保留已有合格英文条目。英文卡名来自 `db.json` 的 `en_name` 分词，或测试/自定义条目的本地名称；未声称恢复了导入器已删除的所有官方标点。

## 日文名称来源

- 143 张真实卡片通过[百鸽公开接口](https://ygocdb.com/api)按真实卡号查询，并校验返回的 `id` 与非空 `text.jp_name`。
- 3 种项目衍生物从 [YGOResources](https://db.ygoresources.com/about/api) 的对应来源卡日文效果中查证：スタッグトークン、クロックトークン、デモンスミストークン。未查询项目合成的“来源卡号 + 1”。
- 卡牌和怪兽共享按 CardId 建立的名称映射；描述、能力、事件中的名称引用采用同一结果。
- 电子多变龙的三个自定义形态使用核实后的基础卡名加译出的形态说明。占位符、测试标签按其用途翻译；语言无关的 `oiiaiooiiai` 与 `Playmaker` 保留。
- 146 份名称证据、主名称键、引用键、日期和核对页面见 [localization-name-sources.json](localization-name-sources.json)。没有未解决的真实卡片或衍生物名称。

## 验证结果

- 两种语言的文件与 key 覆盖、key 顺序均与中文一致，无重复 key、非字符串值或空译文。
- 已比较变量、格式器、嵌套表达式、条件分支数量、BBCode 标签数量与字面反斜杠换行。英文既有 `LARGE_CAPSULE` 的单复数格式器作为已有合法差异保留。
- 已检查全部主卡名/怪兽名与来源记录一致，英文文本未发现遗留汉字。
- Godot 导出临时 PCK 后，在独立验证工程中加载该 PCK，22 个语言文件全部能以 JSON 读取，且文本与源文件逐字一致，两种语言各 812 项。
- 导出时出现 .NET 构建日志及编辑器设置目录的沙箱权限警告；这次验证仅证明语言资源打包和读回成功，不作为 DLL 构建或游戏运行验证。独立读回进程在取得所需日志目录权限后正常退出。
- `git diff --check` 通过。未安装测试资源包到游戏，未验证游戏内字体、换行、升级预览、悬浮提示及选择界面。

## 中文源与动态文本的现有限制

- `cards.json` 中 `CYBERDARK_WURM`、`CYBER_LARVA`、`CYBER_KIRIN`、`CYBER_PHOENIX` 的 description 使用字面量 `\\n`。两种译文均保留这一源格式，没有静默修复。
- `powers.json` 的 `{YgoInfo}` 是动态字符串。`Scripts/Powers/YgoPower.cs` 调用 `CoreCard.GetFormatedInfo()`，从 `db.json` 的中文 `types` 生成资料，且无信息时直接写入中文。此内容无法仅靠翻译现有语言表改变，仍可能显示中文；本次未修改相关 C# 或核心数据。
- 本记录的“全部覆盖”仅指现有中文本地化表的 812 个条目，不代表已将代码、场景或核心数据中的所有硬编码中文抽取为本地化键。
