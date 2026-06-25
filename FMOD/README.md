# STS2 FMOD Minimal Project / STS2 FMOD 最简工程

## Overview / 项目简介

**EN**  
This project is a minimal required FMOD Studio project cleaned and reconstructed from reverse-engineering Slay the Spire 2 FMOD resources.  
It is intended as a stable starting point for mod authors who need to add custom audio content while remaining compatible with the game's existing mixer and bank structure.

**中文**  
本项目是基于 Slay the Spire 2 的 FMOD 资源，经逆向整理后得到的最简必需工程。  
它可作为模组音频开发的稳定起点，方便在不破坏游戏原有混音结构与 Bank 体系的前提下，添加自定义音频内容。

---

## Core Rule: Do NOT modify Master Bank / 核心规则：不要修改 Master Bank

**EN**  
Do **not** edit or replace anything under `Banks/Master` in a way that changes its identity or role as the master bank.  
Always create a **new bank with a globally unique name** and perform all your audio edits only inside that new bank.

Any of the following operations can break FMOD target resolution (especially mixer references) and may lead to in-game silence:
- modifying Master bank internals in incompatible ways
- deleting and recreating Master
- assigning another bank as the new master

Renaming your own bank is fine. Renaming Master does not solve anything and is strongly discouraged.

**中文**  
请**不要**以任何会改变 `Banks/Master` 身份或职责的方式去编辑、替换 Master。  
正确做法是：新建一个**全局不易重名**的 Bank，并且只在这个新建 Bank 中进行所有编辑。

以下行为都可能导致 FMOD 无法正确识别目标（尤其是混音器引用），最终出现游戏静音：
- 对 Master 做不兼容修改
- 删除后重建 Master
- 指定其他 Bank 为 Master

你自己的 Bank 改名可以；Master 改名没有实际意义，且不建议。

---

## Authoring Workflow / 制作流程

### 1) Import assets and create events / 导入资源并创建事件

**EN**
- Import audio assets in `Assets`.
- Create event paths in `Events` with names/folders unlikely to collide with other mods.
- Assign each custom event to **your newly created bank** so it can be exported correctly.
- For detailed event design (instruments, timeline, parameters, etc.), refer to standard FMOD tutorials.

**中文**
- 在 `Assets` 中导入音频资源。
- 在 `Events` 中创建不易与他人重名的事件路径（包括文件夹层级与事件名）。
- 将自定义事件分配到**你新建的 Bank**，确保其可被正确导出。
- 事件细节配置（乐器、时间线、参数等）请参考常规 FMOD 教程。

### 2) Route events to mixer paths / 将事件挂到混音路径

**EN**  
In `Window -> Mixer Routing`, drag your events under appropriate mixer paths.  
This ensures your custom audio follows in-game volume categories and effects correctly.

**中文**  
在 `Window -> Mixer Routing` 中，将你的事件拖拽到对应的混音路径下。  
这样你的音频才会正确跟随游戏内对应类型的音量控制与效果链。

---

## Build Output / 构建输出

**EN**
- `File -> Build` (default output: `Build\desktop\`) generates:
  - `Master.bank`
  - `Master.strings.bank`
  - `<YourBankName>.bank`
- `File -> Export GUIDs` (default output: `Build\`) generates:
  - `GUIDs.txt`

**中文**
- `File -> Build`（默认输出到 `Build\desktop\`）会生成：
  - `Master.bank`
  - `Master.strings.bank`
  - `<你的Bank名>.bank`
- `File -> Export GUIDs`（默认输出到 `Build\`）会生成：
  - `GUIDs.txt`

---

## What to package in your mod / 模组中应该打包哪些文件

**EN**  
In most mod workflows, you only need:
- `<YourBankName>.bank`
- `GUIDs.txt`

Then load them through RitsuLib APIs:
- `FmodStudioServer.TryLoadBank` for `.bank`
- `FmodStudioServer.TryLoadStudioGuidMappings` for `GUIDs.txt`

**中文**  
通常只需要取用以下两个文件打包进你的 mod：
- `<你的Bank名>.bank`
- `GUIDs.txt`

并通过 RitsuLib API 加载：
- `FmodStudioServer.TryLoadBank` 用于加载 `.bank`
- `FmodStudioServer.TryLoadStudioGuidMappings` 用于加载 `GUIDs.txt`

---

## Collision & Rebuild Notes / 冲突与重建注意事项

**EN**
- If your bank name or GUIDs collide with someone else's mod, loading may fail or resolve to wrong targets.
- Always use a uniquely named custom bank.
- Never rely on editing Master to "fix" collisions.
- After any bank/event change, rebuild and re-export `GUIDs.txt`.
- Recommended: refresh `GUIDs.txt` on every build to keep mappings synchronized with event paths.

**中文**
- 若你的 Bank 名称或 GUID 与其他模组发生碰撞，可能出现加载失败或映射错位。
- 务必使用唯一命名的自定义 Bank。
- 不要通过修改 Master 来“规避冲突”。
- 只要改动了 Bank 或事件，就需要重新 Build 并重新导出 `GUIDs.txt`。
- 建议每次 Build 都更新一次 `GUIDs.txt`，确保映射始终与事件路径一致。

---

## Quick Checklist / 快速检查清单

**EN**
- [ ] Created a uniquely named custom bank  
- [ ] Did not modify/replace Master role  
- [ ] Assigned all custom events to custom bank  
- [ ] Routed events in Mixer Routing  
- [ ] Rebuilt banks and exported latest `GUIDs.txt`  
- [ ] Packaged only required files for mod loading

**中文**
- [ ] 已创建唯一命名的自定义 Bank  
- [ ] 未修改/替换 Master 的职责  
- [ ] 所有自定义事件都已分配到自定义 Bank  
- [ ] 已在 Mixer Routing 完成路径挂载  
- [ ] 已重新 Build 并导出最新 `GUIDs.txt`  
- [ ] 已按需打包文件并在 mod 中加载
