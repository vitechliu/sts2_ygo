---
name: design-events
description: Manually invoked workflow for designing and implementing VYgo custom events from user-provided story, options, rewards, values, availability, and art direction. Use only when the user explicitly invokes $design-events to create or revise an event, including its EventModel code, RitsuLib registration, Chinese events localization, optional custom relic dependencies, AI-generated 3440x1616 event illustration, and build or visual verification.
---

# Design Events

Turn a supplied event concept into a registered, localized, illustrated VYgo event. Treat the user's story and option effects as authoritative. Fill presentation gaps, but never silently rebalance or change outcomes.

## Preferred input

Accept natural language or a parameter block containing any of these fields:

```text
中文名：
英文名（可选）：
出现范围：全章节 / 指定 Act
联机选择：个人 / 共同投票
开场内容：
选项：每项的标题、效果、数值、条件、结果
奖励：固定模型 / 随机池 / 需要新建的卡牌或遗物
美术元素：地点、主体、情绪、主色、必须出现或避免的内容
交付范围：只设计 / 代码与本地化 / 完整实现并生成底图
```

Do not require the user to repeat fields already clear from the request. Convert prose into this internal shape and continue.

## Resolve the request

1. Extract the Chinese title, optional English identifier, opening narrative, options, exact effects and values, result text, availability, prerequisites, reward models, and visual motifs.
2. Normalize the English identifier to concise PascalCase without an `Event` suffix unless needed for clarity. Derive the public entry as `V_YGO_EVENT_<SNAKE_CASE_NAME>` and the art stem as lowercase snake case.
3. Interpret availability explicitly:
   - all acts: `[RegisterSharedEvent]`;
   - one or more act models: one `[RegisterActEvent(typeof(...))]` per act;
   - multiplayer voting: override `IsShared`; this is separate from shared-pool registration.
4. Ask one concise question only when a missing choice changes implementation materially, such as an unspecified act restriction or whether an absent reward should be a new custom relic. Otherwise state reasonable assumptions and continue.
5. If the user requests design only, return the completed event specification, localization draft, and art prompt without editing files or generating art.

## Research current patterns

1. Read `references/implementation-guide.md` before implementing code or localization.
2. Read `references/art-direction.md` before generating or evaluating art.
3. Inspect `git status` and preserve unrelated changes.
4. Resolve `STS2_VANILLA_ROOT` and `RITSULIB_ROOT` from the active `AGENTS.md`, environment variables, or an explicit user path. Treat both as read-only.
5. Search the current game and RitsuLib source for the closest event effects and exact command signatures. Do not guess APIs from memory.
6. Inspect 3-6 relevant original event illustrations from `${STS2_VANILLA_ROOT}/images/events/` with `view_image`. Select by similar scene, focal subject, lighting, or palette; learn visual grammar without copying their composition or distinctive subject.

## Finalize the event design

Before editing, settle these decisions and mention non-obvious assumptions in commentary:

- event class, public entry, act registration, and multiplayer behavior;
- opening page, option titles, option descriptions, exact command effects, prerequisites, and result pages;
- fixed versus random rewards and duplicate-reward behavior;
- DynamicVars and whether values are fixed, randomized with event `Rng`, or derived from player state;
- art focal subject, environment, emotional beat, accent color, and right-side UI-safe area.

Keep options mechanically legible. Let art imply the dilemma rather than rendering literal UI labels or two equal poster panels.

## Implement code and localization

1. Create `Scripts/Events/<EventClass>.cs` and inherit `ModEventTemplate`.
2. Use RitsuLib registration attributes. The project already registers its mod assembly; do not add a centralized event registry or Harmony patch.
3. Put displayed values in the narrowest matching DynamicVars and reference the same names in localization.
4. Implement effects through current game commands. `ThatDoesDamage(...)` and similar option decorators only describe danger; they do not apply the effect.
5. End terminal choices with `SetEventFinished(PageDescription("<PAGE>"))`. Use `SetEventState` only for a real follow-up page with further options.
6. Use `Owner` only on mutable event instances. Do not access it from canonical construction or `IsAllowed`.
7. For a fixed custom relic, ensure it is already registered before calling `RelicCmd.Obtain<T>`. If a new relic is part of the requested complete event, invoke `$design-relics` with the finalized reward specification before finishing the event.
8. For a random relic, pull it inside the chosen callback unless consuming it on event entry is intentional. Do not consume the relic grab bag merely to preview an unchosen reward.
9. Add or update `VYgo/localization/zhs/events.json` with title, initial description, option title and description pairs, and every result-page description. Keep page and option tokens stable.
10. Polish narrative localization: use concise second-person scene writing, short action-led option titles, mechanically exact option descriptions, and brief consequence-focused result text. Use rich-text tags sparingly and never hide a cost or prerequisite in flavor prose.
11. Parse the localization JSON after editing.

## Generate the event illustration

Perform this stage when the user requests art, asks for a complete event, or supplies visual direction for the event background.

1. Build a production prompt from `references/art-direction.md`. Bind every placeholder to the finalized event narrative; do not rely on a generic style-only prompt.
2. Invoke `$imagegen` or the built-in `image_gen` tool for a new raster illustration. Generate no typography, icons, card frames, dialogue boxes, borders, or interface elements.
3. Keep generated sources in a temporary directory outside `VYgo/`. Do not overwrite an existing event image unless the user requested replacement.
4. Ask for an ultrawide 2.13:1 composition. Preserve the main subject within the left focal zone and keep the right UI zone dark, simple, and low contrast.
5. Finalize the selected source with:

   ```powershell
   python .agents/skills/design-events/scripts/finalize-event-art.py `
     --input <generated-source> `
     --output VYgo/images/events/<event_stem>.png `
     --preview <temporary-preview>.png
   ```

   If the active Python lacks Pillow, call `codex_app__load_workspace_dependencies` and use its bundled Python.
6. Inspect both the 3440x1616 result and the 1720x808 preview with `view_image`. Reject or regenerate when the subject enters the text area, the right half becomes busy or bright, the focal accent disappears, or the image reads as photorealistic, 3D, anime key art, or soft airbrushed concept art.
7. Fix semantic, composition, lighting, or style failures by regenerating with one targeted prompt change. Use the finalizer only for deterministic crop and resize, not to rescue a poor composition.
8. Keep only the final integrated image in `VYgo/images/events/`. Do not hand-write Godot `.import` or `.uid` files.

## Validate

1. Confirm class name, namespace, public entry, registration, localization keys, result pages, art stem, and asset path agree.
2. Confirm every option text matches the implemented order, amount, target, and reward.
3. Run the finalizer with `--check-only` on the final image and confirm `3440x1616` RGB or RGBA PNG.
4. Run the repository's required publish command:

   ```powershell
   dotnet publish VYgo.csproj -c ExportRelease -f net9.0 -o ./bin/ExportRelease/net9.0/publish
   ```

5. When possible, provide the direct in-run test command:

   ```text
   ritsulib debug event <PUBLIC_ENTRY>
   ```

6. Report assumptions, event behavior, art direction, generated prompt summary, changed files, JSON result, publish result, and remaining in-game checks.

## Guardrails

- Do not run this workflow implicitly; it is intended for explicit `$design-events` invocation.
- Do not modify vanilla or third-party source and localization directories.
- Do not write local absolute paths into this Skill, its references, scripts, or committed project files.
- Do not copy a vanilla event illustration, its exact focal geometry, or distinctive character design.
- Do not put text or UI into generated art.
- Do not invent incomplete English gameplay localization unless the user requests it.
- Do not overwrite unrelated worktree changes or existing event art without explicit replacement scope.
- Do not call a relic-option helper and assume it grants the relic; verify the callback performs the actual command.
