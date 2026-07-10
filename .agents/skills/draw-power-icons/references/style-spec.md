# VYgo Power icon style specification

## Acceptance target

- Final asset: RGBA PNG, exactly 256x256.
- Runtime reuse: the same asset must stay readable at 64x64.
- Canvas: transparent, with at least 12px clear padding at 256px.
- Composition: one centered dominant silhouette occupying roughly 55-75% of the canvas.
- Shape budget: 2-5 large masses; at most three large accents.
- Palette: about 4-10 perceptually distinct colors, dominated by large flat regions.
- Edges: intentional antialiasing, no chroma fringe, no fuzzy halo.
- Line weight: important outlines and gaps should resolve to at least 3px at 64px, equivalent to 12px in the 256px master.

Use original STS2 icons only as structural reference: bold silhouette, simple metaphor, flat colors, and little texture. Do not copy an existing icon's subject or exact geometry.

## Semantic construction

Express the effect as `subject + action/state`:

- Strength gain: weapon, fist, muscle, or upward force + expansion/glow.
- Self damage/destruction: core, heart, shell, or machine + crack/rupture.
- Draw/card manipulation: card silhouette + pull, echo, split, or recycle motion.
- Minion/monster effects: head, claw, egg, or portal + the actual state change.

Prefer a concrete object over a generic aura. If the Power has multiple mechanics, depict the mechanic the player must notice when deciding what to do next.

## Image generation prompt template

```text
Use case: stylized-concept
Asset type: Slay the Spire 2 Power UI icon, designed for 64x64 display and delivered as a 256x256 transparent PNG
Primary request: <one concrete subject performing or showing one action/state>
Style/medium: bold flat-color 2D game icon, vector-like shapes, hand-drawn character, 4-10 colors, no texture
Composition/framing: centered single silhouette, 70-80% of the square source, 2-5 large masses, generous clear padding
Line/readability: dark outline and essential internal gaps must remain at least 3 pixels wide after reduction to 64x64; exaggerate the silhouette
Color palette: <dominant color>, <secondary color>, <one high-contrast accent>
Scene/backdrop: perfectly flat solid <#00ff00 or #ff00ff> chroma-key background
Constraints: uniform background with no shadow, gradient, texture, floor, reflection, or lighting variation; do not use the key color in the subject
Avoid: text, letters, numbers, watermark, frame, badge, scenery, realistic rendering, fine detail, thin strokes, tiny sparks, smoke, blur, glow outside the silhouette
```

Generate at 1024x1024 when possible, then downsample once through `tools/power_icon_pipeline.py`. Do not ask the model to render the final 64px asset directly.

## 64px visual test

View the preview at native size and at 4x nearest-neighbor zoom. Pass only when:

1. The effect metaphor is recognizable in under one second.
2. The silhouette still reads in monochrome.
3. No required shape collapses below 3px.
4. Foreground and internal gaps remain distinct.
5. Transparent edges have no green/magenta/black fringe.

