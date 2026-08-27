# VYgo event illustration art direction

## Contents

1. Visual identity
2. Composition and UI-safe zones
3. Palette and rendering
4. Prompt construction
5. Negative prompt
6. Evaluation rubric

## Visual identity

Describe the target as a **horizontal dark-dungeon event illustration / 2D roguelike scene illustration**. The defining combination is:

- ultrawide environmental storytelling;
- a small-to-medium focal subject on the left third;
- extensive near-black negative space and framing silhouettes;
- one localized high-saturation accent;
- hand-painted hard-edged shadows and simplified angular planes.

The image is a game event backdrop, not a splash poster. It should create atmosphere behind title, narrative text, and option buttons. Do not copy any existing event's subject, exact silhouette, or layout.

## Composition and UI-safe zones

Final canvas: `3440x1616`, approximately `2.13:1`.

Use normalized zones as guidance:

- `0-15%` width: optional near-black foreground occlusion or vignette.
- `15-46%` width: main narrative focal zone; place the subject center around `28-38%` width and `48-68%` height.
- `46-82%` width: UI-safe zone; keep it dark, low-contrast, and low-detail. Broad environmental shapes are acceptable, but no face, glowing object, sharp edge cluster, or narrative focal point.
- `82-100%` width: dark continuation or framing silhouette; avoid a bright terminal edge.

Keep foreground silhouettes irregular and organic enough to frame depth without turning into a decorative border. The focal subject may be smaller than expected; the light pool, directional lines, and environmental geometry should lead the eye toward it.

Avoid symmetrical dual-choice layouts, centered hero compositions, equal left/right lighting, close-up portraits, and full-frame detail. The game's text panel occupies the middle-right area even though it is not part of the generated image.

## Palette and rendering

Use a restricted palette:

- 70-85% deep navy, violet-black, soot brown, or desaturated blue-green shadow;
- 10-25% muted local color describing stone, metal, wood, water, vegetation, or architecture;
- 2-8% saturated focal accent such as cyan flame, molten orange, toxic green, ritual crimson, or relic gold.

Prefer:

- crisp painted shapes with deliberate hard transitions;
- chunky angular silhouettes and readable value grouping;
- sparse rim light and selective highlights;
- layered foreground, focal midground, and simplified background;
- subtle brush texture inside large shapes without noisy microdetail;
- color temperature contrast concentrated near the event's decisive object.

Avoid photorealism, 3D rendering, glossy materials everywhere, cinematic depth-of-field blur, watercolor wash, soft airbrush gradients, dense line art, anime character key art, generic mobile-game splash art, and uniformly bright fantasy concept art.

## Prompt construction

Build one coherent prompt from the following slots. Replace every bracketed field.

```text
Create a 3440x1616 ultrawide horizontal event-background illustration for a dark 2D roguelike dungeon game.

Scene and story: [physical location, time, atmosphere, and the exact moment before the player's choice].
Primary focal subject: [one character, creature, altar, object, or environmental phenomenon], positioned in the left third around 32% of the canvas width, medium-small in scale, with a clearly readable silhouette.
Choice symbolism: [visual details that imply the options without labels, split panels, or literal UI].
Environment: [2-4 supporting architectural or natural elements], layered into dark foreground silhouettes, a readable midground light pool, and a simplified background.
Lighting: [single motivated light source], illuminating only the focal subject and a narrow patch of ground; the rest falls into large hard-edged shadow masses.
Palette: mostly [shadow hue family] with muted [material colors], plus one localized high-saturation [accent color] accent at the focal point.
Composition: approximately 2.13:1, extensive dark negative space, irregular near-black edge framing, focal mass confined mainly to 15-46% width. Keep 46-82% width dark, low-contrast, low-detail, and visually quiet for event title, narrative text, and option buttons. No important face, object, glow, or sharp texture in that UI-safe zone.
Rendering style: hand-painted 2D roguelike event illustration, simplified angular forms, crisp hard-edged cel-painted shadows, chunky silhouettes, restrained brush texture, selective rim light, limited values, dramatic but not photorealistic.
No text, letters, numbers, logos, watermark, interface, dialogue box, card frame, border, or icons.
```

Do not name a living artist or ask to reproduce a particular original event illustration. Use inspected originals only to calibrate layout, value grouping, and UI-safe darkness.

## Negative prompt

Append or reinforce these exclusions when the generator drifts:

```text
Avoid centered composition, symmetrical scene, two-panel choice layout, bright right side, detailed right-side subject, close-up portrait, photorealism, 3D render, smooth airbrushed gradients, glossy cinematic concept art, anime key visual, comic panel, dense outlines, excessive particles, full-frame clutter, readable writing, decorative border, HUD, buttons, or menu elements.
```

When correcting a failure, change one cause at a time. Examples:

- subject too centered: move the focal subject to 30% width and add a dark rightward falloff;
- right side too busy: replace right-side objects with two or three broad shadow planes;
- image too soft: request larger flat value shapes and crisp hard-edge shadow boundaries;
- accent too widespread: restrict saturated light to the focal object and immediate reflections;
- looks like a poster: reduce subject scale and emphasize environmental negative space.

## Evaluation rubric

Accept the image only when all essential criteria pass:

1. **Narrative read:** the location and central temptation, threat, or mystery read without text.
2. **Left focal hierarchy:** the eye lands in the left third before exploring the rest.
3. **UI-safe right side:** middle-right remains readable behind light-colored title and body text.
4. **Dark-space ratio:** large dark masses dominate without collapsing the focal silhouette.
5. **Accent discipline:** one accent color carries the story and occupies only a small fraction of the canvas.
6. **Hard-edged 2D rendering:** shapes feel painted and graphic, not photographic, 3D, or softly airbrushed.
7. **Ultrawide crop safety:** no essential subject is cut during conversion to 3440x1616.
8. **No generated UI or writing:** no text-like marks, frames, badges, or interface artifacts.

Inspect the 1720x808 preview as a proxy for in-game viewing. If the event description becomes difficult to imagine over the right half, the art is not ready.
