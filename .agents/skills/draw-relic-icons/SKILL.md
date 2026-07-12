---
name: draw-relic-icons
description: Create, process, validate, and integrate transparent 256x256 Relic icons for the VYgo Slay the Spire 2 mod. Use when adding or replacing art for a class under Scripts/Relics, when BaseYgoRelic.AssetProfile needs its class-named PNG, or when checking whether a Relic icon matches the bold readable style of VYgo Power icons and original STS2 relics.
---

# Draw Relic Icons

Create one transparent 256x256 PNG per Relic. Save it as `VYgo/images/relics/<RelicClassName>.png`, preserving the exact case of the C# class name. `BaseYgoRelic.AssetProfile` reuses that file for `IconPath`, `IconOutlinePath`, and `BigIconPath`, so design one asset that reads clearly at both 256x256 and the small in-game size.

## Workflow

1. Read the Relic class and its localization. Base the object and metaphor on the actual effect, not only the class name. If either source is missing, report that assumption before generating art.
2. Derive the output filename from the concrete C# class name, including a final `Relic` suffix. Do not translate, lowercase, or convert it to snake case. Example: `CyberCoreRelic` becomes `VYgo/images/relics/CyberCoreRelic.png`.
3. Inspect 3-6 relevant original icons in `D:/github/raw107/images/relics/`. Choose references by similar object category, silhouette, or palette. Use them only to learn visual grammar; do not copy their subject, exact geometry, or distinctive details. Never modify that directory.
4. Read [references/style-spec.md](references/style-spec.md), then form a production prompt from its template.
5. Use the built-in `image_gen` tool at square resolution. Request a perfectly flat chroma-key background; use `#00ff00` unless the subject is green, then use `#ff00ff`.
6. Copy the selected generated source into `/tmp/vygo-relic-icons/`. Keep generated/keyed sources out of `VYgo/`.
7. Reuse the shared icon finalizer with a Python that has Pillow:

   ```bash
   python3 tools/power_icon_pipeline.py finalize \
     --input /tmp/vygo-relic-icons/<relic>-source.png \
     --output VYgo/images/relics/<RelicClassName>.png \
     --preview /tmp/vygo-relic-icons/<relic>-64.png \
     --report /tmp/vygo-relic-icons/<relic>-report.json \
     --transparent-threshold 32 \
     --crop-alpha-threshold 24 \
     --padding 12 \
     --max-color-bins 36
   ```

   The Power finalizer is intentionally shared because both asset types have the same transparency, 256px canvas, and small-size readability requirements. Relics deliberately use tighter padding and a higher color-bin limit to retain painted material shading. If faint keyed pixels make the subject unexpectedly small, raise `--transparent-threshold` or `--crop-alpha-threshold`; do not compensate by upscaling a badly cropped result. If plain `python3` lacks Pillow in Codex desktop, call `codex_app__load_workspace_dependencies` and use its bundled Python executable.
8. Inspect the 256 output and 64 preview. The dominant subject should occupy about 85-92% of the canvas width or height after normalization. The 64px preview is a stricter proxy for the roughly 85x85 small Relic display. Reject ambiguous silhouettes, lost internal gaps, muddy gradients, color-key fringe, or outline segments thinner than about 3px.
9. Run strict validation:

   ```bash
   python3 tools/power_icon_pipeline.py check VYgo/images/relics/<RelicClassName>.png \
     --max-color-bins 36 \
     --strict
   ```

10. Confirm the exact filename matches the Relic class used at runtime. `BaseYgoRelic` already provides the asset paths; only add or override `AssetProfile` when the class does not inherit that convention. Do not hand-write Godot `.import` or `.uid` files.
11. Run `dotnet build`. If Godot resources were newly added, prefer the project publish flow before release.

## Iteration rules

- Fix semantic or composition failures by regenerating with one targeted prompt change.
- Fix only outer silhouette thickness with `finalize --stroke 3 --stroke-color '#2b1720'`; do not use stroke as a substitute for a readable source design.
- Never quantize, posterize, or reduce the palette merely to pass the color-bin heuristic. Preserve intentional highlights, shadows, and material depth; regenerate only when the shading is genuinely noisy or muddy at 64px.
- Never upscale a small icon into the 256px master.
- Never overwrite an existing icon unless the user asked for replacement; use a temporary candidate filename while comparing variants.
- Keep only the final transparent 256px PNG in `VYgo/images/relics/`. Treat the 64px image and JSON report as disposable QA artifacts.
- Do not create a separate outline image under the current `BaseYgoRelic` convention; the same class-named PNG serves all three asset paths.

## Naming examples

- `CyberCoreRelic` -> `CyberCoreRelic.png`
- `DuelDiskRelic` -> `DuelDiskRelic.png`
- `BaseYgoRelic` -> `BaseYgoRelic.png`
