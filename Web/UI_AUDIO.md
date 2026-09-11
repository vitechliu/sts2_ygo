# UI 音效替换

后台「音效替换」管理原版 FMOD 事件与自定义工程事件的对应关系。当前只开放通用按钮 7 项、地图 5 项、时间线 7 项。同一事件在按钮、卡牌选择等位置共享替换；不修改音乐入口。

## 使用

1. 启动原有后台：在 `Web` 中执行 `npm install`、`npm start`。
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
