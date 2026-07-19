---
name: design-cards
description: Design, implement, and verify VYgo card scripts from a target C# path and an effect specification. Use when Codex needs to add or modify a card under Scripts/Cards, research analogous effects in this mod or vanilla STS2 Chinese card/relic localization, map vanilla localization model IDs to read-only source, choose exact commands and lifecycle hooks, implement DynamicVars and upgrades, update card/minion localization or paired monster code, and build-check the result.
---

# Design Cards

Turn a target card path and effect description into a working, localized VYgo implementation. Treat the requested effect as the source of truth; use existing mod and vanilla implementations to learn engine APIs and lifecycle semantics, not to replace the design.

## Resolve the request

1. Read [references/implementation-guide.md](references/implementation-guide.md) completely.
2. Normalize the supplied path relative to the repository and inspect the file when it exists. Preserve its class name, `CardId`, base class, cost, type, rarity, target, pool registrations, starter registrations, stats, and art behavior unless the user asks to change them.
3. Translate the effect into an explicit contract: trigger, timing, target, amount, duration, limit, zone, randomness or selection rule, upgrade delta, and whether the action mutates a collection.
4. Derive omitted facts from the target file, paired monster, localization, and neighboring cards when there is one clear answer. State material assumptions. Ask one concise question only when different answers would produce meaningfully different gameplay and the repository cannot resolve the choice.
5. For a missing target file, search the intended category, `VYgo/db.json`, pools, and localization before deciding metadata. Do not invent a YGO `CardId`, registration pool, starter count, or monster stats when they cannot be discovered.

## Research before coding

1. Search this project first. Inspect the target base class and the closest cards, minions, relics, powers, utilities, and commands with the same trigger or payoff.
2. Search vanilla Chinese localization for semantic matches in both cards and relics. Prefer two to four distinctive effect fragments over a broad single word. Run the bundled read-only helper, for example:

   ```powershell
   & .agents/skills/design-cards/scripts/find-vanilla-effects.ps1 -Query '抽.*牌', '丢弃' -Kind All
   & .agents/skills/design-cards/scripts/find-vanilla-effects.ps1 -ModelId ACROBATICS -Kind Card
   ```

3. Open the returned vanilla `.cs` files and compare at least the closest implementation. Inspect a relic too when it demonstrates the required lifecycle or persistent state better than a card.
4. Resolve the vanilla reference root through the current `AGENTS.md`, the `STS2_VANILLA_ROOT` environment variable, or an explicit user-provided path as described in the implementation guide. Never hard-code a machine-specific absolute path in this tracked skill. Ensure the configured localization and source belong to the game version targeted by the project.
5. Copy verified method signatures, command composition, ownership guards, and reset boundaries. Do not copy vanilla balance values or assume that a localized sentence reveals every implementation detail.

## Place the behavior correctly

- Put immediate play resolution in the card's `OnPlay`.
- Put effects that exist only while a summoned monster is in play—attacking, taking damage, turn callbacks, death, or persistent modifiers—in the paired `Scripts/Monsters/YGO/*Minion.cs` model. Update both files when the contract crosses that boundary.
- Use card callbacks such as draw, combat-entry, or card-play hooks only when the card itself must react while it is in the relevant zone. Apply owner and combat-state guards to global callbacks.
- Reuse project utilities and command APIs. Verify every hook and command against the current source or an already compiling project example before using it.
- Snapshot an enumerated collection with `ToList()` before the effect removes, sacrifices, moves, or destroys its members.

## Implement the card

1. Choose the narrowest project base class and keep constructor metadata internally consistent.
2. Register through attributes; do not add a handwritten central card registry.
3. Put player-facing numeric values in concrete `CanonicalVars` entries and reference the same variables from localization. Preserve the monster card's inherited `AttackVar` and `LifeVar` when extending `CanonicalVars`.
4. Implement upgrades in `OnUpgrade`. Upgrade the exact DynamicVar, cost, stat, or rule described by the user; do not silently rebalance the effect.
5. Match `TargetType` to actual target handling. Validate nullable targets before use and pass the current `PlayerChoiceContext` through asynchronous commands.
6. Add only hover tips required by mechanics in the description. Preserve the base summon hover tip for monster cards.
7. If a new monster/minion pair is introduced, keep the shared `CardId`, namespace, cache discovery, scene/image conventions, and localization identity aligned.

## Update localization

1. Edit `VYgo/localization/zhs/cards.json` as UTF-8. Derive the prefix from the class name as uppercase snake case with `V_YGO_CARD_`; for example, `CyberDragon` becomes `V_YGO_CARD_CYBER_DRAGON`.
2. Keep `.title` and `.description` synchronized with code. Add fields such as `.selectionScreenPrompt` only when the implementation consumes them.
3. Describe exact timing, targets, limits, randomness, and upgrade-visible values. Use the implemented DynamicVars rather than hard-coded localized numbers where practical.
4. If behavior resides in the paired minion and its UI text also needs to change, update `VYgo/localization/zhs/monsters.json` or other matching localization in the same change.
5. Parse every edited JSON file after modification and preserve unrelated keys and UTF-8 Chinese text.

## Validate and report

1. Review the diff for path/class/namespace, `CardId`, base class, registrations, target type, DynamicVars, upgrade behavior, localization keys, and paired minion consistency.
2. Run `dotnet build`. Fix errors caused by the implementation; do not modify read-only vanilla source or unrelated user changes.
3. When no automated test covers the behavior, identify the smallest in-game scenario that verifies trigger timing, target selection, upgrade behavior, and zone transitions.
4. Report the vanilla/project references used, implemented semantics, assumptions, changed files, JSON validation, build result, and any remaining in-game visual or behavioral check.

## Guardrails

- Do not guess APIs from memory when local source is available.
- Do not stop after writing a plausible method body; integrate registration, variables, upgrade behavior, localization, and paired monster logic required by the effect.
- Do not change cost, rarity, target, stats, pool, starter loadout, or effect wording beyond the requested scope without explaining why.
- Treat the resolved vanilla reference root, including its `src/` and `localization/` directories, as read-only.
- Do not hand-edit generated Godot `.uid` or `.import` files unless the task specifically requires an asset workflow.
