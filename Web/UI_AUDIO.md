# UI 音效替换

后台「音效替换」管理原版 FMOD 事件与自定义工程事件的对应关系，提供通用按钮 7 项、地图 5 项、时间线 7 项。使用 `/#audio` 直接打开页面；分类可折叠，也可只显示已配置项。同一事件在游戏各处统一替换。

## 使用

1. 在 `Web` 执行 `npm install` 或 `npm ci`，然后 `npm start`。安装过程自动准备固定版本 FMOD WASM。
2. 在「试听环境」填写原版 desktop bank 目录，需包含 `Master.bank`、`Master.strings.bank`、`sfx.bank`。也可通过环境变量 `FMOD_ORIGINAL_BANK_DIR` 设置初始值。机器路径只保存在忽略提交的 `Web/audio-settings.local.json`。
3. 使用 FMOD Studio 制作事件并构建 desktop bank、导出 `GUIDs.txt`。在「新建 / 更新替换工程」填写名称、bank 目录、文件名和 GUID 文件路径。未提供 GUID 文件时，需一并导入工程的 strings bank。
4. 选择替换事件，分别点击「原版试听」「替换后试听」，确认后保存。重新选择再保存可修改映射；「撤销编辑」丢弃未保存的选择；「取消替换」恢复原版。
5. 重新构建工程后，可选择已有工程更新。新 bank 必须保留仍在使用的事件；删除事件前先取消对应映射。
6. 按项目原有方式发布 Mod 并重启游戏。后台保存不会热更新正在运行的游戏。

## 运行方式和依赖

正式试听在浏览器内使用 FMOD Studio WASM 直接播放 bank，通过 Web Audio 输出。Node 服务提供配置和资源，并使用同一份官方 WASM 枚举、检查导入事件；管理和试听都不需要 Python、游戏 FMOD DLL 或服务器声卡。旧的原生渲染脚本、WAV 接口和临时 WAV 处理已移除。

| 用途 | 需要准备 |
| --- | --- |
| 换电脑运行后台 | 项目、已导入 bank/映射、Node.js/npm、原版 bank；安装依赖并重新填写本机 bank 路径 |
| 浏览器试听 | 支持 WebAssembly、Web Audio/AudioWorklet 的现代浏览器；通过 localhost 或 HTTPS 打开 |
| 制作、更新 bank | FMOD Studio；仅试听不需要安装 Studio |
| 编译、发布 Mod | 项目要求的 .NET 9 和 Godot；二者不参与网页试听 |

后台默认绑定本机 `127.0.0.1`。原版 bank 只读使用，不复制进 Mod。页面初次点击才初始化引擎和获取 bank：当前原版三个 bank 合计 44,672,550 字节，WASM 初始内存配置 256 MiB。后续试听复用同一引擎与已加载 bank，自定义事件按需补充资源。资源以内容 SHA-256 标识，更新后重新初始化；浏览器 HTTP 缓存可复用未变化文件，但没有预渲染音频缓存。

开始新试听立即停止上一实例；加载中的旧请求可取消；停止按钮只停止当前试听。切换离开音效标签或离开页面会卸载 bank、释放引擎并关闭音频上下文。返回后重新初始化。短音效按工程默认参数播放，最长八秒，音量滑块只控制网页试听。

## 工程兼容性与游戏接入

- 使用兼容 FMOD 2.03 的 bank。建议沿用 `FMOD/STS2.fspro` 的音效总线结构，不重复打包原版 Master/sfx；独立工程必须带齐总线、样本与 strings/GUID 依赖。导入时会共同加载原版、已有 VYgo 和已导入 bank 检查冲突。
- 缺少运行库、加载失败、bank 不兼容及 FMOD 返回的插件/依赖错误会显示在页面，修复后可直接重试。需要特殊参数、程序员音效回调或额外插件的事件不保证可独立试听，应制作成默认参数即可播放的短 UI 音效。
- 单个 bank 限制 128 MiB，组合资源限制 192 MiB；它们是资源大小限制，不是浏览器实际内存占用保证。
- 自定义事件使用独立路径。含 `/music/` 或 `/bgm/` 的事件不会进入 UI 选择列表，也不能保存为替换目标。
- 导入文件保存到 `VYgo/banks/ui/`，与内置 VYgo bank 内容相同则引用已有文件。映射保存到 `VYgo/audio/ui-replacements.json`。更新采用新目录，保留旧资源以免失败破坏已有配置；确认不再使用的目录可另行清理。
- 原版清单与映射编译嵌入 DLL；bank/GUID 文件通过现有 Godot 发布流程打包。保留导出过滤器的 `*.bank,*.guids.txt`。
- 游戏的 `NAudioManager.PlayOneShot(string, Dictionary<string,float>, float)` Harmony 前缀统一改写路径，并保留原调用的参数、音量与后续 RitsuLib 播放流程。等待所有延迟加载回调结束，且目标 GUID 实际存在时才启用映射；加载失败保留原版。

### 与主菜单背景音乐的复用

主菜单 BGM 和 UI 音效共用现有 `VYgo.bank`、GUID 注册和 RitsuLib 延迟 bank 加载服务，额外 UI 工程也使用同一去重注册机制。两者均在延迟初始化完成后检查真实 GUID。主菜单保留 `NAudioManager.PlayMusic` 及其主音量/BGM 音量、循环和场景生命周期；UI 保留单次 `PlayOneShot`。本改动不修改主菜单音乐内容、开关或播放时机，也不将 BGM 加入这 19 项清单。

## 固定运行库的准备和 Git 边界

运行库为 FMOD 2.03.08，来自 [FMOD 官方公开 HTML5 示例](https://www.fmod.com/assets/html5/studio_api/load_banks.html)。`Web/fmod-runtime.json` 记录两个文件的来源、大小、SHA-256 和实测版本。JS 为 131,434 字节，WASM 为 2,656,574 字节。它们不是 npm 包，`postinstall` 调用本项目准备脚本下载。

```powershell
cd Web
npm ci                   # 自动准备运行库
npm run prepare:fmod     # 手动准备、校验或失败后重试
```

两个文件存放在忽略提交的 `Web/vendor/fmod/2.03.08/`。完整匹配时直接复用，不访问网络；缺失或损坏时下载到临时目录，整套校验通过才替换版本目录。失败保留先前版本，清理临时下载并明确提示重试。官方示例 URL 不带版本号，固定摘要拒绝上游静默变更；升级须重新实测并有意更新配置。使用 `--ignore-scripts` 安装后需要手动准备。

服务端提供依赖时再次验证摘要，浏览器校验 JS 完整性和实际引擎版本。第三方 JS/WASM 不纳入 Git；公开可下载不代表任意再分发授权，应遵循项目适用的 FMOD 许可。

## 验证

`npm test` 覆盖清单边界、映射保存/取消、运行库准备与失败保护。设置 `VYGO_AUDIO_INTEGRATION=1` 和 `FMOD_ORIGINAL_BANK_DIR` 后运行 `node --test test/uiAudioService.test.js`，使用真实 WASM 验证 19 项枚举、已有 VYgo 工程导入/更新、播放资源清单和不兼容 bank 反馈。持久化测试使用临时目录，不覆盖用户方案。

浏览器验证使用正式播放器与正式 API：设置原版 bank 环境变量后运行 `node test/browser/server.js`，打开输出的本机地址，执行两个验证按钮。已有工程需包含 `event:/vygo/sfx/material_shine`。分析器接在真实 Web Audio 输出上，记录 peak/RMS、结束状态和 bank 请求数；另验证停止后无输出、快速切换、释放后重建、取消加载、缺依赖、坏 bank、初始化失败和恢复。故障仅注入测试页面，不改运行库或用户配置。结果写到忽略提交的 `Web/.audio-cache/wasm-browser-results.json`。完成后停止测试服务。

2026-09-12 实测：正式播放器的 19 项原版事件及现有 `material_shine` 全部非静音且正常结束。原版 bank 首次请求 3 个，后续切换无需重新请求；自定义音效仅新增 1 个 bank。停止、连续切换、释放重建、加载取消和四类故障恢复共 8 项场景全部通过；正式页面的两种试听、已配置筛选、离开后返回试听也通过。该结果覆盖本次桌面 Chromium 与上述工程，不表示所有浏览器、插件或参数化工程均兼容。游戏代码经构建/发布验证，未将网页结果冒充游戏内试听验收。
