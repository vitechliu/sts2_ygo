# UI 音效替换

后台「音效替换」管理原版 FMOD 事件与自定义工程事件的对应关系。当前只开放通用按钮 7 项、地图 5 项、时间线 7 项。同一事件在按钮、卡牌选择等位置共享替换；不修改音乐入口。可用 `/#audio` 直接进入该标签页。

## 使用

1. 启动原有后台：克隆仓库后，在 `Web` 中执行 `npm install`（或 `npm ci`）、`npm start`。安装依赖时会自动准备固定版本的 FMOD 网页运行库；克隆本身不执行脚本。
2. 在「试听环境」填写游戏目录、原版 desktop bank 目录和 64 位 Python 3 程序。原版目录需包含 `Master.bank`、`Master.strings.bank` 和 `sfx.bank`。也可以通过 `FMOD_DLL_DIR`、`FMOD_ORIGINAL_BANK_DIR`、`FMOD_PYTHON` 提供初始值。机器路径仅保存在忽略提交的 `Web/audio-settings.local.json`。
3. 在 FMOD Studio 创建自定义事件，构建 desktop bank 并导出 `GUIDs.txt`。在「新建 / 更新替换工程」填写名称、打包目录、需要导入的 bank 文件名以及 GUID 文件路径。没有 GUID 文件时，需导入该工程的 strings bank。
4. 选取原版事件对应的自定义事件，试听后保存。修改对应关系可直接重新选择保存；「撤销编辑」丢弃尚未保存的选择，「取消替换」恢复原版。
5. 修改工程并重新构建后，选择已有工程更新。仍在使用的事件必须保留；需要删除事件时先取消对应替换。
6. 运行项目原有发布命令并重启游戏。后台保存不会热更新正在运行的游戏。

## 工程与兼容性

- 本实现使用游戏目录中的 `fmod.dll` / `fmodstudio.dll`，需要 Windows、64 位 Python 3 和兼容 FMOD 2.03 的 bank，不另外分发 FMOD SDK。
- 试听通过 FMOD Studio API 加载真正的 bank，使用离线 WAV 输出，不使用服务器扬声器。浏览器收到 WAV 后通过页面播放器播放；「停止试听」、切换标签页和开始另一项试听都会停止前一次播放并取消未完成请求。
- 试听使用默认参数，最长约八秒。缺少样本、bank 版本不兼容、插件缺失等会显示错误；纯静音渲染也会报错。需要特殊参数、程序员音效回调或外部插件的事件，应在工程中先改为能独立播放的短 UI 音效。
- 建议从项目现有 `FMOD/STS2.fspro` 的混音结构制作 UI bank，沿用原版音效总线。不要重复打包原版 Master 或 sfx；独立工程必须带齐自己的总线与样本依赖。导入会与原版 bank、已导入 bank 和现有 VYgo bank 共同加载验证；同 GUID 的不同 bank 构建不允许同时使用。
- 自定义事件须使用独立路径。含 `/music/` 或 `/bgm/` 的事件不会出现在选择列表，也不能保存为 UI 替换。
- 导入资源复制到 `VYgo/banks/ui/`，映射保存到 `VYgo/audio/ui-replacements.json`。更新采用新目录，保留旧资源以避免在保存失败时破坏旧配置；确认不再使用后可由开发者清理旧目录。
- 原版清单及映射随编译嵌入 DLL，bank 与 GUID 文件由原有 Godot 发布流程打包。请保留导出过滤器的 `*.bank,*.guids.txt`。
- 游戏通过 `NAudioManager.PlayOneShot(string, Dictionary<string,float>, float)` 的 Harmony 前缀统一改写事件路径，沿用参数与音量。仅在延迟音频初始化后确认目标 GUID 已加载时启用；失败时保留原版。

## 验证

`npm test` 包含清单边界和持久化检查。设置 `VYGO_AUDIO_INTEGRATION=1` 以及上述 FMOD 环境变量后运行 `node --test test/uiAudioService.test.js`，可实测全部 19 项原版事件、自定义 bank 导入更新、非静音 WAV 和错误反馈。集成测试使用独立临时目录，不会覆盖用户方案。

FMOD 离线输出参数参见[官方 System API](https://www.fmod.com/docs/2.03/api/core-api-system.html#fmod_outputtype)。

## 换电脑使用

| 使用方式 | 需要准备 |
| --- | --- |
| 完整迁移后台 | 项目及已导入的 bank/映射、Node.js 与 npm 依赖、64 位 Python 3、游戏的两个 FMOD DLL、原版 bank，重新填写本机路径 |
| 只用另一台电脑的浏览器访问原服务器 | 渲染依赖仍放在原服务器，客户端只需浏览器；但默认 `127.0.0.1` 仅本机可访问，当前没有开放局域网 |
| 制作和更新自定义 bank | 使用 FMOD Studio；网页试听本身不需要安装 Studio |
| 编译、发布游戏 Mod | 项目要求的 .NET 9 和 Godot 构建环境；它们不参与网页试听渲染 |

Python 只使用标准库，无需额外安装音频 Python 包。游戏原版 bank 只读使用，不复制进 Mod。

当前没有持久化的预览缓存。每次点击都重新渲染短 WAV，HTTP 发送完成后删除临时文件；浏览器只保留当前声音的临时 Blob，在停止、换声音或离开页面时释放。因此首次生成和下一次点击都需要服务器具备完整渲染环境，不能依靠之前听过的声音省略搬机依赖。

## WASM 可行性实测（2026-09-12）

已通过独立本机网页验证，当前后台仍使用上面的原生渲染方案，尚未切换为 WASM。

- 从 [FMOD 官方公开 HTML5 示例](https://www.fmod.com/assets/html5/studio_api/load_banks.html)正常取得其 `fmodstudio.js` 和 `fmodstudio.wasm`，没有账号登录或绕过下载限制；运行库报告版本为 2.03.08。
- 在浏览器内直接加载同一批原版 `Master.bank`、`Master.strings.bank`、`sfx.bank`，成功查到并播放 `event:/sfx/ui/clicks/ui_click`。验证服务只提供静态文件，原生试听服务当时已关闭。
- 浏览器 Web Audio 输出分析器测得峰值 `0.4401763677597046`、最大 RMS `0.08238652248673708`；出现 2 个播放通道，事件状态 `0 → 4 → 2`（播放、停止过程、已停止），AudioContext 为 `running`，无错误。
- 原版三个 bank 合计 44,672,550 字节，WASM 为 2,656,574 字节。本次探针配置 256 MiB WASM 初始内存；这不是测定的最低需求。
- 此结论只覆盖上述原版 `ui_click`，尚未证明所有自定义工程、插件、参数或浏览器都兼容。原生路线的 19 项全部通过，不应写成 WASM 的 19 项全部通过。
- 若后续正式采用 WASM，试听可移除 Python 和游戏 DLL 的渲染依赖，由浏览器加载 bank 播放；后台仍需提供 bank 和配置服务。正式集成及运行库分发需按项目使用的 FMOD SDK 许可办理，本次只进行了公开示例的本机兼容性验证，未把第三方运行库加入仓库。

### 运行库依赖形式与 Git 边界

运行库由配套的 `fmodstudio.js`（131,434 字节，JavaScript 加载和接口绑定）与 `fmodstudio.wasm`（2,656,574 字节，编译后的运行库）组成。这两个文件不是独立的 npm 包；`postinstall` 调用本项目准备脚本，从官方公开示例的明确来源取得文件，没有引入或假定存在某个官方 npm 包。

运行库安装在 `Web/vendor/fmod/2.03.08/`，整个 `Web/vendor/fmod/` 已由 Git 忽略。早期验证文件所在的 `Web/.audio-cache/` 也已忽略。仓库仅保存 `fmod-runtime.json` 中的版本、来源、大小和 SHA-256，以及准备脚本、测试和说明；第三方 JS/WASM 均不追踪、不提交。公开可下载不等于允许任意再分发。

```powershell
cd Web
npm ci                   # 自动执行 postinstall，准备运行库
npm run prepare:fmod     # 手动准备、校验或失败后重试
```

准备过程会先校验本地两个文件，完全匹配时直接复用，不访问网络。缺失或损坏时下载到临时目录，校验整套文件后再替换版本目录；下载或校验失败会清理临时目录并保留此前版本，npm 返回失败并给出重试提示。官方示例 URL 不含版本号，因此固定摘要用于拒绝静默升级；若上游内容变化，需先重新实测版本并有意更新配置，不会绕过校验自动接受新文件。使用 `npm install --ignore-scripts` 会跳过自动准备，之后应手动运行上述命令。

这一步仅准备已验证可用的 WASM 依赖，尚未将当前试听页面切换为 WASM，也不会替代当前原生试听所需的 Python 和游戏 DLL。
