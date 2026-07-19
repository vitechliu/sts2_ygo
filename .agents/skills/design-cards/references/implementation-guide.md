# VYgo card implementation guide

## Contents

- [Project map](#project-map)
- [Configure the vanilla reference root](#configure-the-vanilla-reference-root)
- [Select a base class](#select-a-base-class)
- [Search vanilla by localized effect](#search-vanilla-by-localized-effect)
- [Convert the effect into engine behavior](#convert-the-effect-into-engine-behavior)
- [Commands and patterns to verify](#commands-and-patterns-to-verify)
- [DynamicVars and upgrades](#dynamicvars-and-upgrades)
- [Registration, IDs, and localization](#registration-ids-and-localization)
- [Validation checklist](#validation-checklist)

## Project map

| Concern | Location or convention |
|---|---|
| Card scripts | `Scripts/Cards/Category/<Category>/<ClassName>.cs` |
| Monster models | `Scripts/Monsters/YGO/<ClassName>Minion.cs` |
| Card pools | `Scripts/Pools/` |
| Card localization | `VYgo/localization/zhs/cards.json` |
| Monster localization | `VYgo/localization/zhs/monsters.json` |
| YGO data | `VYgo/db.json`, loaded through `Entry.CoreCardCache` |
| Card art | `res://VYgo/images/cards/<CardId>.png` |
| Monster art/scene | `res://VYgo/images/monster/<CardId>.png`, `res://VYgo/scenes/monsters/<CardId>.tscn` |
| Vanilla localization | `${STS2_VANILLA_ROOT}/localization/zhs/cards.json`, `relics.json` |
| Vanilla source | `${STS2_VANILLA_ROOT}/src/Core/Models/Cards/`, `Relics/` |

Always pair localization and source from the same vanilla version. The current `BaseMonsterCard` uses the 0.109 `GetResultLocationForCardPlay` API, so configure a 0.109 reference dump unless the project dependency version changes.

## Configure the vanilla reference root

Resolve the local reference root in this order:

1. Use an explicit `-VanillaRoot` supplied by the caller.
2. Otherwise read the `STS2_VANILLA_ROOT` environment variable.
3. Otherwise search upward for the nearest `AGENTS.md` and read an exact `STS2_VANILLA_ROOT: <path>` configuration line.
4. If none provides a valid directory, ask for the path instead of guessing or scanning unrelated filesystem locations.

The resolved root must contain both `src/` and `localization/`. Keep it outside the mod's write scope and treat it as read-only. Never commit a developer's absolute path to this skill or to shared examples.

This repository ignores `AGENTS.md`, so each computer may store its own value without changing tracked files:

```text
STS2_VANILLA_ROOT: /path/to/sts2-reference-root
```

Configure it per shell or in the developer's persistent environment:

```powershell
$env:STS2_VANILLA_ROOT = '<path-to-sts2-reference-root>'
```

```bash
export STS2_VANILLA_ROOT='/path/to/sts2-reference-root'
```

The helper also accepts `-VanillaRoot` for a one-off invocation and `-AgentsPath` to inspect a non-default instructions file. Advanced callers may override `-LocalizationRoot` and `-SourceRoot` separately; explicit directory parameters take precedence over the root.

## Select a base class

| Design | Base class | Notes |
|---|---|---|
| Main-deck monster | `BaseMonsterCard` | Skill-type card that summons a paired minion and moves to `Entry.MonsterPile` |
| Fusion monster | `BaseExtraFusionCard` | Extra deck; override material requirements where needed |
| Link monster | `BaseExtraLinkCard` | Extra deck; link markers and material rules are card-specific |
| Normal spell | `BaseSpellCard` | Choose `CardType`, `CardRarity`, and `TargetType` in its constructor |
| Summon action | `BaseSummonCard` | Reuse only for extra-deck summon flows matching its contract |
| Other YGO frame behavior | `BaseVYgoCard` | Override `CardYgoType` only when no narrower base fits |

Read the complete base-class chain before overriding behavior. In particular, preserve `BaseMonsterCard` summon flow, result location, playability cap, `AttackVar`, `LifeVar`, and summon hover tip.

## Search vanilla by localized effect

The vanilla localization files are flat JSON maps:

```text
ACROBATICS.title       -> 杂技
ACROBATICS.description -> 抽牌并丢弃
```

The portion before the first dot is the model ID. It normally maps from uppercase snake case to a PascalCase source filename: `ACROBATICS` to `Acrobatics.cs`, `ART_OF_WAR` to `ArtOfWar.cs`.

Use the bundled helper from the repository root:

```powershell
# Every regex must match somewhere in the same model's title/description fields.
& .agents/skills/design-cards/scripts/find-vanilla-effects.ps1 -Query '抽.*牌', '丢弃' -Kind All -MaxResults 20

# Use an explicit one-off root when the environment variable is not set.
& .agents/skills/design-cards/scripts/find-vanilla-effects.ps1 -VanillaRoot '<path-to-sts2-reference-root>' -ModelId ACROBATICS -Kind Card

# Resolve one known localization key/model ID directly.
& .agents/skills/design-cards/scripts/find-vanilla-effects.ps1 -ModelId ART_OF_WAR -Kind Relic
```

Search cards for immediate command composition and upgrades. Search relics for lifecycle hooks, per-turn/per-combat state, owner checks, and reset boundaries. Open each returned source file; the helper is discovery, not implementation evidence by itself.

If a model ID does not resolve automatically, search source filenames by important English tokens and inspect the class:

```powershell
$cardSource = Join-Path $env:STS2_VANILLA_ROOT 'src/Core/Models/Cards'
$relicSource = Join-Path $env:STS2_VANILLA_ROOT 'src/Core/Models/Relics'
rg --files $cardSource | rg 'Acrobatics|Discard'
rg --files $relicSource | rg 'ArtOfWar'
```

## Convert the effect into engine behavior

Before editing, write down:

| Dimension | Examples |
|---|---|
| Trigger | play, draw, summon, attack, death, turn start/end, combat start/end |
| Subject/owner | this card, its owner, any player, the summoned monster |
| Target | selected enemy, random enemy, self, hand card, combat pile card |
| Value | damage, block, cards, energy, power stacks, attack/life |
| Lifetime | this action, this turn, this combat, while summoned, permanent |
| Limit | once per turn, once per combat, first matching event, unlimited |
| Zone | hand, draw, discard, exhaust, monster pile, extra pile |
| Upgrade | numeric delta, cost reduction, target/rule change, stats |

Use the narrowest implementation site:

- `OnPlay` for the card's immediate resolution.
- Card lifecycle callbacks for state or cost belonging to the card itself.
- The paired minion for summoned-monster attacks, damage reactions, death triggers, turn behavior, and persistent effects.
- A Power model only when the game state must outlive the resolving card/minion and existing project or vanilla patterns support it.

For callbacks observable by multiple players or objects, check `Owner`/card ownership and combat state. Reset mutable limits at the exact boundary they describe. Do not emulate a per-turn rule with a per-combat flag.

## Commands and patterns to verify

Find a current compiling example before using any command. Common starting points include:

| Effect | Look for |
|---|---|
| Damage | `DamageCmd.Attack(...).FromCard(...).Targeting(...).Execute(...)` |
| Draw | `CardPileCmd.Draw(choiceContext, amount, Owner)` |
| Hand/pile selection | `CardSelectCmd` plus `CardSelectorPrefs` |
| Move/discard/exhaust | `CardCmd` or `CardPileCmd` methods in current-version vanilla/project examples |
| Add a generated card | `CreateClone()` and `CardPileCmd.AddGeneratedCardToCombat` |
| Apply powers | `PowerCmd` patterns using the exact target and source signatures |
| Temporary card cost | `EnergyCost.SetUntilPlayed`, `SetThisTurn`, or `SetThisCombat` according to lifetime |
| Animation | Existing `CreatureCmd`, hit VFX, and VYgo `VFXUtil` patterns |

Never infer a command overload from a mismatched game version. Build failures often indicate a versioned API signature; check the configured current-version reference and current project usages first.

## DynamicVars and upgrades

Prefer typed variables:

| Meaning | Typical variable |
|---|---|
| Damage | `DamageVar` with the correct `ValueProp` |
| Block | `BlockVar` |
| Cards | `CardsVar` |
| Energy | `EnergyVar` |
| Power stacks | `PowerVar<TPower>` after verifying its generated name |
| Monster attack/life | project `AttackVar`, `LifeVar` |
| Custom value | `DynamicVar` or an existing project-specific subclass |

Monster cards overriding `CanonicalVars` must include `AttackVar` and `LifeVar` plus any new variables. `OnUpgrade` must update the same instances displayed in localization. Use `BaseValue` or the project's established accessor when commands require a numeric value; do not bypass modifiers accidentally.

When an upgrade changes rules instead of a number, keep the branch explicit and ensure the localized upgraded rendering is supported. Check vanilla examples for `IfUpgraded` only after confirming the current localization formatter.

## Registration, IDs, and localization

- Use `[RegisterCard(typeof(<Pool>))]` and optional `[RegisterCharacterStarterCard(typeof(<Character>), <Count>)]`.
- Keep existing registration untouched unless the task changes card availability.
- `CardId` links the card to YGO data and art. A paired card/minion must share it.
- Check `Entry.BuildYgoIdCaches()` or its current discovery mechanism when adding a brand-new minion type.
- Derive localization by converting the class name to uppercase snake case and prefixing `V_YGO_CARD_`.
- Use `.selectionScreenPrompt` only when code reads `SelectionScreenPrompt`.
- Include required hover tips for keywords or custom mechanics, but avoid duplicating automatically supplied tips.

## Validation checklist

- Target path, namespace, class name, and localization prefix agree.
- Constructor cost/type/rarity/target agree with actual behavior.
- Registration and starter counts remain intended.
- `CardId`, card art, paired minion, monster art, and scene agree.
- `CanonicalVars`, command values, descriptions, and upgrades agree.
- Global callbacks guard owner/combat and reset state correctly.
- Mutated collections are snapshotted before iteration.
- Edited JSON parses as UTF-8.
- `dotnet build` succeeds.
- The report identifies a focused in-game check when runtime behavior cannot be automated.
