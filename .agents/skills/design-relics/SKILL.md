---
name: design-relics
description: Design and implement VYgo relics from a Chinese name, optional English name, and effect description. Use when Codex needs to translate or normalize a relic name, create its C# model under Scripts/Relics, register it with a character relic pool or starter loadout, add Chinese title/description/flavor entries to VYgo/localization/zhs/relics.json, verify the implementation, and optionally invoke draw-relic-icons for matching 256x256 art.
---

# Design Relics

Turn a relic concept into a registered, localized VYgo relic. Treat the user-provided effect as the source of truth, but implement it through existing STS2 lifecycle hooks and project conventions rather than inventing APIs.

## Resolve the specification

1. Collect the Chinese display name, effect, rarity, owning character or relic pool, starter status, and optional flavor text or icon direction.
2. If the user provides an English name, use it as the identifier source. Otherwise translate the Chinese name into concise idiomatic English.
3. Convert the English name to PascalCase and append `Relic` exactly once. Preserve acronyms only when they are established names. Example: `电子核心` / `Cyber Core` becomes `CyberCoreRelic`.
4. If rarity is omitted, infer it only when the effect clearly implies one and state the assumption. If the owning pool or starter status cannot be discovered from context, ask one concise question before editing because those choices change registration.
5. Before writing code, state any non-obvious mapping among Chinese name, English name, class name, rarity, and pool.

## Implement the relic

1. Read [references/implementation-guide.md](references/implementation-guide.md).
2. Inspect `Scripts/Relics/BaseYgoRelic.cs`, the target pool under `Scripts/Pools/`, and the closest existing mod relic.
3. Resolve the read-only vanilla reference root from the active `AGENTS.md` instructions or `STS2_VANILLA_ROOT`, then search `${STS2_VANILLA_ROOT}/src/Core/Models/Relics/` for one or more original relics with the same trigger, state lifetime, or reward. Ask for the root if it is not configured. Copy the lifecycle pattern and method signature, not the original balance or theme. Never modify that directory.
4. Put starter relics in `Scripts/Relics/Starters/`. For other relics, reuse an existing appropriate subfolder; if none exists, place the file directly in `Scripts/Relics/` rather than inventing a new one-item taxonomy.
5. Inherit `BaseYgoRelic`, add `[RegisterRelic(typeof(<TargetRelicPool>))]`, and override `Rarity`.
6. For a starter relic, also add `[RegisterCharacterStarterRelic(typeof(<Character>))]`. Prefer registration attributes over editing obsolete `StartingRelicTypes` collections.
7. Override `CanonicalVars` in every concrete relic that exposes numeric values. Use the narrowest matching `DynamicVar` type and make localization reference the same variable. Override with `[]` when the relic intentionally has no variables so it does not inherit unrelated base defaults.
8. Implement the effect with exact engine hook signatures. Check ownership and combat state where applicable, call `Flash()` when the relic activates, use `AssertMutable()` in state setters, and reset per-turn, per-combat, or per-run state at the matching lifecycle boundary.
9. Add hover tips only when the description introduces mechanics that are not already expanded automatically.

## Add localization

1. Derive the localization prefix from the class name: `CyberCoreRelic` becomes `V_YGO_RELIC_CYBER_CORE_RELIC`.
2. Edit `VYgo/localization/zhs/relics.json` as UTF-8 and add exactly these keys:

   - `<PREFIX>.title`
   - `<PREFIX>.description`
   - `<PREFIX>.flavor`

3. Write the title from the Chinese name. Write a concise effect description that exactly matches the implementation, including trigger limits and timing.
4. Use existing STS2 rich-text conventions and the concrete relic's DynamicVars. Do not hard-code a localized number when a DynamicVar supplies it.
5. Use the user's flavor text when provided. Otherwise write one short thematic line; do not reuse the deprecated placeholder flavor.
6. Parse the JSON after editing. Preserve existing entries and UTF-8 Chinese text.

## Handle the icon conditionally

- Invoke `$draw-relic-icons` after code and localization are stable when the user requests art, asks for a complete integrated relic, supplies visual references, or the agreed scope includes the missing icon.
- Pass the finalized class name, Chinese title, implemented effect, and any visual references to that Skill.
- Expect the result at `VYgo/images/relics/<RelicClassName>.png`; `BaseYgoRelic.AssetProfile` supplies all three runtime paths automatically.
- If art is outside scope, do not generate a placeholder. Report the exact missing icon path instead.

## Validate

1. Confirm the class, namespace, file path, registration attributes, target pool, rarity, DynamicVars, localization prefix, and icon filename all agree.
2. Parse `VYgo/localization/zhs/relics.json` with a JSON parser.
3. Run `dotnet build`.
4. When an icon was created, follow `$draw-relic-icons` validation and do not hand-write Godot `.import` or `.uid` files.
5. Report assumptions, implemented trigger semantics, changed files, build result, and whether icon work was performed or deferred.

## Guardrails

- Do not implement only localization when the user requested a working effect.
- Do not silently change the user's trigger, amount, limit, target, or duration for balance reasons.
- Do not guess lifecycle method signatures; verify them from current source or dependency metadata.
- Snapshot collections with `ToList()` before an effect mutates them.
- Preserve unrelated worktree changes and avoid editing generated Godot metadata by hand.
