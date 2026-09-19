# 大师决斗独立特效预览

本阶段只提供预览，不注册怪兽攻击、召唤或其他正式游戏触发。范围为源目录 `packs` 中的 **74 个独立包、507 个粒子系统、41 个 Shader**。另外 62 个需要脚本或演出控制的入口保存在 `pending_controlled.json`，没有独立包的数据不能凭空补成可播放演出。

## 启动

在项目根目录运行：

```powershell
.\tools\preview-master-duel-effects.ps1
```

如果已经位于 `tools` 目录：

```powershell
.\preview-master-duel-effects.ps1
```

脚本兼容 Windows PowerShell 5.1，保存为 UTF-8 BOM。它读取项目 `env.props` 的 GodotPath，构建 C#、导入资源，再打开 `master_duel_effect_browser.tscn`。预览构建使用 `SkipModInstall=true`，不会复制 DLL、清单或 PCK 到游戏安装目录。控制台等待窗口关闭；错误保留在当前终端，运行日志为 `.context/master-duel-effects/preview.log`。

浏览器默认列出全部 74 项，支持源名称搜索、分类筛选、上/下一项、重播、暂停、停止、播放全部、重复、速度和随机种子。提供自动/正面/俯视/侧面/斜视、缩放和灰色背景。自动取景只是方便观察的预览相机，不是原作相机。

带 `↻` 的条目保留源循环。播放全部每项停留 4–10 秒后切换；手动选择的循环效果持续播放，直到停止或切换。攻击 S1/S2 拖尾及按距离发射的效果提供「移动演示」，默认关闭；勾选后使用明确的测试轨迹，不把这条轨迹当作原动画。

## 资源与实现

- `Core/Effects/MasterDuel/`：C# 数据资源、曲线求值、粒子模拟、几何生成、独立视口和浏览器。
- `VYgo/scenes/vfx/master_duel/<id>_3d.tscn`：可复用 Node3D 场景；支持 `Play(seed)`、`Stop()`、暂停和播放速度。
- `<id>.tscn`：带相机的独立预览包装。`<id>/source.tres` 显式引用材质并携带标准化源数据。
- `VYgo/shaders/master_duel/generated/`：41 个源 Shader 对应的 44 个前向 Shader 变体；不是旧 Raw 工程的占位 Shader。
- `VYgo/images/vfx/master_duel/`：去重后的原始 PNG，不重画源贴图。
- `source/`：74 包原 `effect.json` 和 41 套 Shader 证据，包括 315 段真实反汇编及参数二进制。原始 HLSL/ShaderGraph 仍未恢复。
- `manifest.json`：每项实际输入文件、SHA-256、材质使用的源 Shader/程序、目标 Shader 和混合/深度状态。此证据目录有 `.gdignore`。

保持项目 Godot 4.5.1、C#/.NET 9、Mobile 渲染器。运行时重建父子节点、活动状态、平移/四元数/缩放；Unity 到 Godot 使用 Z 镜像，原始几何 JSON 的 UV、法线、颜色、切线和索引参与渲染。闲置网格字段不用于 Billboard 的绘制或取景。

固定 120 Hz 模拟处理出生延迟、Burst 数量/时间/重复/概率、按时间和距离发射、寿命、循环/预热、本地/世界空间、三种缩放模式、形状、Hermite 尺寸与旋转曲线、颜色/alpha 渐变、速度/轨道/径向速度、限速、阻力、噪声、出生子发射器继承与粒子拖尾。保留禁用 Renderer 和 RenderMode=None；它们不会因为存在粒子而被强行画出来。

序列帧按原网格和寿命曲线选帧；自定义顶点流按源顺序连续打包至 TEXCOORD，包含 UV/UV2、稳定随机值、年龄、Custom1/Custom2 和 NoiseSum。本批未出现的流类型会报错。Mesh 粒子使用导出的 `.geometry.json`，不使用只供静态预览的 OBJ。

Shader 转换读取 DXBC 输入/输出签名、常量缓冲区字节偏移、变体参数 blob、材质关键字和渲染状态，逐指令生成顶点/片元程序。保留材质浮点/颜色、纹理 ST、UV 运算、顶点色、自定义流、条件分支、噪声运算、采样 bias、加色/普通透明/不透明、Cull、ZWrite 和 ZTest。Shader 时钟跟随播放/暂停/重播。视口使用普通合成，在内部完成透明混合，避免再次乘 alpha 或把所有效果强制加色。

前向表现是本阶段的目标；源 MotionVectors、DepthNormals、ShadowCaster 等辅助 Pass 保留为证据，不作为 Godot 的额外绘制 Pass。未知指令、常量绑定或不支持的材质状态会中止导入，不静默替换成通用贴图 Shader。

## 再生成与检查

```powershell
python tools/import_master_duel_effects.py --source <Effects源目录>
python tools/validate_master_duel_effects.py --source <Effects源目录>
.\tools\preview-master-duel-effects.ps1 -Verify
python tools/validate_master_duel_effects.py --runtime-report .context/master-duel-effects/all-74/results.json
```

导入器只读取源目录，输出限定本仓库。资源验证检查 74 项、507 个系统、41 个源 Shader、场景/材质/纹理引用及 4483 条输入散列。自动运行检查在实际 Mobile GPU 渲染中逐项以 1× 播放，检查粒子生成、几何、非背景像素与停止清理，并保存每项 5 张运行截图及计时。它只检查每项开头 4–10 秒，不冒充所有长循环和完整演出的验收。

## 明确的重建与待验证范围

**74 项可浏览、可运行，不代表已证明与原作视觉一致。** 当前没有一一对应的原作视频或 Unity 原场景运行结果；图集仅是 Godot 运行证据。

- Unity 原生噪声使用同参数 Perlin 重建；原生随机序列、阻尼积分、轨道运动、子发射器继承细节和拖尾接缝仍需原引擎对照。保留参数不等于已经恢复 Unity 的原生算法。
- Unity 内建 Cube/Sphere/Plane/Quad 用对应几何重建；内建网格拓扑、拉伸粒子锚点、相机速度拉伸、屏幕尺寸限制及透明排序精度待对照。
- 预览提供正交相机、线性色调映射和背景切换；原相机、背景深度、曝光、后处理和音频未随独立包恢复。软粒子的背景深度交互、HDR/色彩空间、纹理过滤/各向异性精度需要原效果验证。
- 自动运行的移动演示用于验证拖尾和距离发射，不代表源动画；62 个受控制入口没有在本阶段虚构脚本、Timeline 或正式用途。
- 未进行联机、不同 GPU/渲染器、多效果并发、长时间循环、游戏内正式流程验证。本轮没有发布游戏包；源码中撤下了前阶段的普通攻击/电子镭射龙触发。

详细的实际执行记录见 `VALIDATION.md`。

实现参考：[Godot 4.5 spatial shader](https://docs.godotengine.org/en/4.5/tutorials/shaders/shader_reference/spatial_shader.html)、[SurfaceTool](https://docs.godotengine.org/en/4.5/classes/class_surfacetool.html)、[Unity 粒子枚举](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/ParticleSystem/Managed/ParticleSystemEnums.cs)、[Unity 曲线求值实现](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/ParticleSystem/Managed/ParticleSystemStructs.cs)。参数 blob 的布局同时以实际导出字节校验，并参考 [参数格式解析资料](https://github.com/Andreansx/casualties-unknown-apple-silicon/blob/main/tools/rewrap/paramblob.py)。
