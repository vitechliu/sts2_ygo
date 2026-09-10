# 2026-09-11 实测记录

本文件用于查证结果及条件化排错，不需要每次普通卡牌测试都读取。

## 最新流程约定

用户在实测后明确修正：本项目当前处于测试阶段，后续沿用当前存档，有未完成对局时直接从主菜单放弃后开始测试，无需每次重问，不切存档。下文存档 3 的选择仅是历史执行记录，不是以后照做的步骤。本次技能整理没有实际执行放弃，也没有恢复游戏操作。

## 游戏与样例

- 工作区 HEAD：`b70d910`；原有 `AM AGENTS_MAC.md` 未修改，未切分支。
- 安装目录 `release_info.json`：游戏 `v0.111.0`、commit `41cef1ea`；安装 VYgo 清单为 `0.3.0`。实际日志确认 VYgo 主菜单布局/音乐加载及 `Raigeki (id=V_YGO_CARD_RAIGEKI)` 注册。
- 安装 DLL 与现有 `.godot/mono/temp/bin/ExportRelease/VYgo.dll` 的 SHA256 一致：`A29632C09B03EC2F16301C47A803163CAC82E2C9C3A505A7B9E8BCDA72CE8DF4`。本次未重新构建或部署，不表示任意后续工作区改动已在游戏内加载。
- 历史测试使用存档 3 的自定义模式、藤木游作、进阶 0、无额外特效；原存档 1 的未完成对局未动。测试局创建后从初始事件用 `fight BOWLBUGS_WEAK` 进入双敌人战斗。
- 代表卡来源：`Scripts/Cards/Category/Common/Raigeki.cs`、`VYgo/localization/zhs/cards.json`；YGO ID `12580477`，控制台模型 `V_YGO_CARD_RAIGEKI`。

| 阶段 | 实际操作/观察 | 结果 |
| --- | --- | --- |
| 启动与主菜单 | `sky.launch_app` 启动已核实的安装 exe，枚举唯一窗口并截图 | 先短暂黑屏，后显示 VYgo 主菜单、档案、新闻与工具栏 |
| 控制台 | Shift+8 打开/关闭；Ctrl+U 清空；type_text 输入后 Return 提交 | 成功；quoteleft 键名不受支持，战斗中 Escape 还会弹暂停 |
| 指定战斗 | `fight BOWLBUGS_WEAK` | 回执 `Jumped to encounter: 'BOWLBUGS_WEAK'`，进入双敌人战斗 |
| 基础雷击 | `card V_YGO_CARD_RAIGEKI`，拖牌实际打出 | 敌人生命 46→39、22→15，各减少 7；能量 3→2；弃牌数 0→1 |
| 升级 | 再生成一张，按当时第六张手牌执行 `upgrade 5` | 回执确认“雷击+”；牌面为 1 费、对所有敌人造成 9 点伤害 |
| 升级雷击 | 悬牌移到更高场地区域释放 | 敌人生命 39→30、15→6，各减少 9；能量 2→1；弃牌数 1→2 |

盛碗虫（石）的“失衡”是其攻击被完全格挡后的效果，不改变本次雷击受伤量。两次测试均在第一回合完成，未点击结束回合。初始生命由当前生成场景决定，不能固定用 46/22 作为下次前提。

## 截图与覆盖范围

本次本地证据根目录为仓库相对路径 `.context/game-test-2026-09-11/`，它不是技能分发必需资源；其他机器没有该目录时不要假定文件存在。

| 已存在文件 | 证据内容 |
| --- | --- |
| `01-console-open.png` | 主菜单控制台打开 |
| `02-profile-slots.png` | 当时查看的存档槽；仅历史记录 |
| `03-custom-setup.png` | 自定义测试设置 |
| `04-fight-command.png` | 指定战斗成功回执 |
| `05-card-generated.png` | 雷击生成成功回执及手牌 |
| `06-before-raigeki.png` | 基础版打出前生命/能量 |
| `07-after-raigeki.png` | 基础版结算后生命/能量/弃牌 |
| `08-upgrade-command.png` | 升级成功回执 |
| `09-upgraded-card-text.png` | 升级牌面中文描述、卡名与费用 |

升级结算的 30/46、6/22、1/3 能量及弃牌 2 已在最后成功工具截图中观察。下一次刷新/保存被用户实体 Escape 中止，**`10-after-upgraded-raigeki.png` 没有写出，不存在该文件证据**。

主菜单截图与升级牌面已实际观察；主菜单有测试新闻文案，未做全面 UI 质量审校。弃牌堆预览、完整 UI 回归、联机、召唤类卡牌、伤害修正、跨回合效果均未验证。也未执行退出测试局或恢复档案。最后观察的游戏状态为存档 3 测试战斗第一回合；后续是否变化应重新观察，不能依赖此历史状态。

## 历史截图故障（已解除）

首次通过 `mcp__node_repl__js` 直接导入 `@oai/sky`，应用枚举、启动和唯一窗口识别成功，但 `get_window_state` 报：

```text
SetIsBorderRequired failed: 不支持此接口 (0x80004002)
```

重新枚举/选择窗口后重试仍相同，共两次捕获请求，无截图返回。当时遵循停止条件，没有发送游戏输入；这不是缺少 native 工具。

随后经用户明确授权，安装 [CodexComputerUseFix v0.1.0](https://github.com/MagicalAstrogy/CodexComputerUseFix/releases/tag/v0.1.0) 的本地兼容层。安装保护测试、8 项 WGC 探针，以及官方 sky 的首次/连续/最大化/还原截图、无截图读取均通过，之后游戏截图和上述卡牌测试也成功。未改 helper 本体、系统 DLL 或安全设置。

只有再次遇到该错误或需要回退时，才查看本机仓库相对路径 `.context/CodexComputerUseFix-download/result.md`、`rollback.md`、安装前后 JSON。这些是本机诊断产物，不保证随仓库存在；缺失时根据当前 computer-use 技能和真实运行环境重新核实，不能猜旧 helper 路径或自动安装第三方补丁。

窗口转换/遮挡中曾有残留、通知或输入法浮层。应重观测稳定画面后判断，不能将一次成功推断为所有应用/遮挡场景均无异常。当前普通测试无需重跑兼容层安装和探针。
