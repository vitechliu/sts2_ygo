---
name: manage-vygo-project
description: Orchestrate the VYgo project from its core Tencent Docs spreadsheet. Use when Codex needs to review, summarize, add, or update global task progress; locate a character card-pool sheet and batch-process cards by status; scaffold cards through Web/scripts/import-card.js; hand each card to $design-cards; or write completion and blocking notes back to the core document.
---

# Manage VYgo Project

Use the core Tencent Docs workbook as the project-management and card-design input, while treating the repository as the source of truth for current APIs and implementation state.

## Load dependencies

1. Use `$tencent-docs` for every workbook read or write. Read its authentication and Sheet workflow instructions before calling `sheet-mcp`; never handle or print its Token directly.
2. Read [references/core-document.md](references/core-document.md) completely before accessing the workbook.
3. Use `$design-cards` for every card implementation after a scaffold or existing target path is resolved. Do not duplicate its research, localization, lifecycle, or build rules here.
4. Stop and report the missing dependency if either Skill is unavailable. Do not replace Tencent Docs with browser scraping or replace `$design-cards` with an improvised implementation flow.

## Establish current state

1. Work from the repository root and inspect `git status` before generating files. Preserve unrelated user changes.
2. Check Tencent Docs authentication as required by `$tencent-docs`. On Windows, if `bash` is not on `PATH`, locate Git for Windows through `git.exe` and invoke its Bash; do not skip the mandated authentication check merely because the bare `bash` command is unavailable.
3. Query the workbook's live sheet list and dimensions. Use the configured sheet IDs as stable hints, but verify the sheet name before acting.
4. Read the header row and resolve columns by exact header text. Never rely only on cached column indexes because the workbook can evolve.
5. Re-read each target row immediately before changing either the repository or the workbook. If the row moved, locate it again by its exact task or card name.

Compare Tencent Docs Skill versions as semantic `MAJOR.MINOR.PATCH` values. Update only when `latest` is greater than the installed version; if the versions are equal but the provider instruction claims an update is required, report the inconsistency and continue without replacing the Skill.

Use this source priority when facts conflict:

1. The user's current request.
2. The target row's card effect, upgrade, cost, rarity, type, and explicit notes.
3. Current repository code, `AGENTS.md`, dependencies, and build output for engine/API facts.
4. The workbook's general rules as design intent. Flag materially stale rules instead of silently forcing them onto current code.

## Coordinate global task progress

Use the `任务进度` sheet for project-wide planning and status reporting.

1. Read all non-empty rows and summarize by priority and status when asked for an overview.
2. Match an existing task by exact `需求/任务`. If multiple rows match, stop and ask which row to use.
3. Preserve existing `详情`, `负责人`, and other fields unless the user requests a replacement. Merge a concise progress note into `详情` rather than discarding useful context.
4. Add a new task only when the user requests it or when the requested workflow clearly requires a new tracked task. Use the first completely empty row; do not reuse a partially populated row.
5. Do not mark a broad character task complete merely because one card or one subset of its card pool is complete. Update its details with counts and remaining work unless its full recorded scope is demonstrably finished.
6. Re-read every changed row and report the final values.

## Develop cards from a character pool

### Build the worklist

1. Resolve the requested character card-pool sheet by exact name, then verify its `sheet_id` against the live workbook.
2. Read the entire used range within the Sheet API cell limit, splitting the request when necessary.
3. Keep rows whose `卡名` is non-empty and whose `状态` exactly matches the requested status, normally `待开发`.
4. Capture at least: row identity, `卡名`, `卡片id`, `类型`, `稀有度`, `费用`, `效果(括弧内为升级效果)`, `备注(特殊升级效果等）`, `状态`, and `AI自动化进度/备注`.
5. Preserve workbook order unless the user requests another priority. Show the user the selected count and names before a large batch only when that preview would materially help them control scope.

### Process each card

1. Re-read the row. If its status no longer matches, skip it and mention the concurrent change.
2. Resolve the repository card pool from the mapping in the reference. Confirm the live importer options when the mapping is missing or has changed.
3. From the repository root, run:

   ```powershell
   node Web/scripts/import-card.js "<卡名>" --pool "<CardPool>"
   ```

4. Treat the importer output as evidence:
   - If it generates a scaffold, capture the exact card C# path and resolved card ID.
   - If the card already exists, locate its current script, paired minion, localization, and ID; continue with `$design-cards` instead of rerunning the importer blindly.
   - If the name has multiple matches, the English class name is unavailable, the pool is invalid, or generation is partial, inspect the diff and follow the blocking protocol below. Never guess an ID or English class name.
5. Invoke `$design-cards` with the exact target C# path and the row's complete effect contract, including cost, rarity, type, upgrade text, special notes, and any user overrides.
6. Let `$design-cards` complete research, implementation, paired minion/Power work, localization, JSON validation, and build verification.
7. After success, re-read the workbook row, then:
   - fill `卡片id` only when it is blank and the importer or repository resolved it unambiguously;
   - set `状态` to `已完成` only when implementation and required validation succeeded;
   - set `AI自动化进度/备注` to a concise completion note including the build result and any remaining in-game check.
8. Read the updated cells back and verify them before moving to the next card.

For three or more workbook writes, follow `$tencent-docs` batch-write rule and use the current `sheet-mcp` batch schema. Do not loop over single-cell writes when a batch update can safely express the same operation.

## Record blockers and notify the user

Write a blocking note to the current card's `AI自动化进度/备注` before stopping or asking the user whenever a critical issue is known. Use this compact form:

```text
阻塞：<阶段>；原因：<可操作的简述>；需要：<用户决定或输入>
```

Critical issues include ambiguous card matches, missing design text that changes gameplay, unresolved ID/class/pool metadata, partial scaffold generation, conflicting user edits, API or authentication failure, systemic build failure, or a required choice that `$design-cards` cannot resolve from the repository.

- Keep `状态` unchanged on failure unless the user defines a dedicated blocked status.
- Never put Tokens, authorization URLs, stack traces, private paths, or long logs in the workbook.
- Continue with other independent cards after a card-local blocker unless the user requested stop-on-first-error.
- Stop the batch for a shared schema, authentication, pool-mapping, dependency, or build-environment problem that could invalidate subsequent cards.
- If `AI自动化进度/备注` is absent, do not silently write into another column. Report the schema blocker and ask whether the workbook should be extended.

## Safe workbook writes

- Change only requested fields plus the required automation note, resolved blank card ID, and justified status transition.
- Verify exact sheet, row, header, and current value immediately before every write.
- Prefer structured or CSV reads that preserve multiline card effects.
- Preserve formatting by editing cell values only.
- Read changed cells back after every logical write group.
- Report what changed in both the workbook and repository, including blocked rows and cards skipped due to concurrent edits.
