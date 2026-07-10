---
name: draw-power-icons
description: Create, process, validate, and integrate transparent 256x256 Power icons for the VYgo Slay the Spire 2 mod. Use when adding or replacing a PowerAssetProfile icon, when a class under Scripts/Powers needs art, or when checking whether a Power icon remains readable at the in-game 64x64 size.
---

# Draw Power Icons

Create one transparent 256x256 PNG per Power and use the same file for `IconPath` and `BigIconPath`. Design for 64x64 first: one dominant silhouette, few large shapes, thick outlines, and large color regions.

## Workflow

1. Read the Power class and its localization. Base the metaphor on the actual effect, not only the class name.
2. Choose one subject and at most three large supporting accents. Avoid text, card frames, circular badges, scenery, particles, thin lines, and tiny decoration.
3. Read [references/style-spec.md](references/style-spec.md), then form a production prompt from its template.
4. Use the built-in `image_gen` tool at square resolution. Request a perfectly flat chroma-key background; use `#00ff00` unless the subject is green, then use `#ff00ff`.
5. Copy the selected generated source into `/tmp/vygo-power-icons/`. Keep generated/keyed sources out of `VYgo/`.
6. Run the deterministic finalizer with a Python that has Pillow:

   ```bash
   python3 tools/power_icon_pipeline.py finalize \
     --input /tmp/vygo-power-icons/<power>-source.png \
     --output VYgo/images/powers/<snake_case>_power.png \
     --preview /tmp/vygo-power-icons/<power>-64.png \
     --report /tmp/vygo-power-icons/<power>-report.json
   ```

   If plain `python3` lacks Pillow in Codex desktop, call `codex_app__load_workspace_dependencies` and use its bundled Python executable.
7. Inspect both the 256 output and the 64 preview. At 64px, reject ambiguous silhouettes, lost internal gaps, muddy gradients, color-key fringe, or outline segments thinner than about 3px.
8. Run strict validation:

   ```bash
   python3 tools/power_icon_pipeline.py check VYgo/images/powers/<snake_case>_power.png --strict
   ```

9. Set both `IconPath` and `BigIconPath` to the final `res://VYgo/images/powers/...png` path. Do not hand-write Godot `.import` or `.uid` files.
10. Run `dotnet build`. If Godot resources were newly added, prefer the project publish flow before release.

## Iteration rules

- Fix semantic or composition failures by regenerating with one targeted prompt change.
- Fix only outer silhouette thickness with `finalize --stroke 3 --stroke-color '#2b1720'`; do not use stroke as a substitute for a readable source design.
- Never upscale a 64px icon into the 256px master.
- Never overwrite an existing icon unless the user asked for replacement; use a temporary candidate filename while comparing variants.
- Keep only the final transparent 256px PNG in the game resource directory. Treat the 64px image and JSON report as disposable QA artifacts.

## Naming

Convert the class name without its final `Power` suffix to snake case and append `_power.png`:

- `SelfDestroyPower` -> `self_destroy_power.png`
- `CyberNetworkPower` -> `cyber_network_power.png`

