# VYgo Relic icon style specification

## Acceptance target

- Final asset: RGBA PNG, exactly 256x256.
- Runtime reuse: the same asset must remain readable at 64x64 and roughly 85x85.
- Canvas: transparent, with 12-20px clear padding at 256px.
- Composition: one centered, tangible object whose bounding box occupies roughly 85-92% of the canvas width or height after background removal.
- Shape budget: 2-6 large masses; at most three large supporting accents.
- Palette: about 4-10 base colors with restrained highlight and shadow variants, dominated by broad color regions. The 64px report may contain up to 36 effective color bins because antialiasing and painted shading create intermediate colors.
- Edges: dark, hand-drawn outlines with intentional antialiasing; no chroma fringe or fuzzy halo.
- Depth: retain deliberate painted highlights and shadows that describe material and curvature; avoid only noisy texture and photorealistic lighting.
- Line weight: important outlines and gaps must resolve to at least 3px at 64px, equivalent to 12px in the 256px master.

VYgo Power icons establish the readability baseline: bold silhouette, simple metaphor, thick outlines, and large color regions. Original icons in `D:/github/raw109/images/relics/` add the Relic grammar: a collectible physical object, slight hand-painted asymmetry, restrained highlights and shadows, and no surrounding badge. Use original icons only as structural reference; never copy an existing icon's subject or exact geometry.

## Semantic construction

Express the mechanic as `collectible object + altered feature`:

- Starting resource or energy: core, battery, coin, disk, or vessel + charge, opening, or stored light.
- Card manipulation: deck box, sleeve, hand, scroll, or device + draw, recycle, split, or selection cue.
- Monster or minion effect: egg, capsule, figurine, collar, or summoning device + the actual state change.
- Defense or survival: shield, armor fragment, charm, or reinforced machine part + the protected area.
- Scaling or repeated trigger: counter, gear train, growing crystal, or linked components + visible accumulation.

Prefer an object that could plausibly be found and carried as a relic. Encode the effect into the object's material, damage, motion, or one attached accent. Avoid depicting a full scene or multiple unrelated symbols.

## Reference selection

Before prompting, inspect 3-6 PNG files from `D:/github/raw109/images/relics/`:

1. Pick at least one reference with a similar silhouette or physical object category.
2. Pick at least one reference with a suitable value structure or palette.
3. Note only reusable traits such as occupancy, outline weight, number of masses, and highlight placement.
4. Do not pass an original icon into image generation as an edit target unless the user explicitly requests a transformation and has the rights to do so.

## Image generation prompt template

```text
Use case: stylized-concept
Asset type: Slay the Spire 2 Relic UI icon, designed for small inventory display and delivered as a 256x256 transparent PNG
Primary request: <one concrete collectible object whose altered feature communicates the relic effect>
Style/medium: bold flat-color 2D game icon with restrained hand-painted shading, thick dark hand-drawn outline, 4-10 colors, slight asymmetry
Composition/framing: centered isolated object, 80-90% of the square source, 2-6 large masses, enough outer space for a clean chroma-key boundary
Line/readability: silhouette and essential internal gaps must remain at least 3 pixels wide after reduction to 64x64; exaggerate the defining feature
Color palette: <dominant color>, <secondary color>, <one high-contrast accent>
Scene/backdrop: perfectly flat solid <#00ff00 or #ff00ff> chroma-key background
Constraints: uniform background with no shadow, gradient, texture, floor, reflection, or lighting variation; do not use the key color in the object
Avoid: text, letters, numbers, watermark, frame, circular badge, scenery, character portrait, photorealism, fine detail, thin strokes, tiny particles, smoke, blur, glow outside the silhouette
```

Generate at 1024x1024 when possible, then downsample once through `tools/power_icon_pipeline.py`. Do not ask the model to render the final small asset directly.

## Small-size visual test

View the 64px preview at native size and at 4x nearest-neighbor zoom. Pass only when:

1. The object and its defining altered feature are recognizable in under one second.
2. The silhouette still reads in monochrome.
3. No required shape collapses below 3px.
4. Foreground and internal gaps remain distinct.
5. Transparent edges have no green, magenta, or black fringe.
6. It still looks like a portable collectible object rather than a Power aura, card illustration, or miniature scene.
7. Metallic, glass, stone, or organic material remains legible through intentional highlights and shadows; do not flatten the image solely to reduce reported color bins.
