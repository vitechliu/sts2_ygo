# VYgo relic implementation guide

## Project mappings

| Concern | Convention |
|---|---|
| Base class | `VYgo.Scripts.Relics.BaseYgoRelic` |
| General registration | `[RegisterRelic(typeof(TargetRelicPool))]` |
| Starter registration | Add `[RegisterCharacterStarterRelic(typeof(TargetCharacter))]` |
| Zane pool | `ZaneTruesdaleRelicPool` |
| Redhat pool | `RedhatRelicPool` |
| Starter directory | `Scripts/Relics/Starters/` |
| Localization | `VYgo/localization/zhs/relics.json` |
| Icon | `VYgo/images/relics/<RelicClassName>.png` |
| Engine references | `D:/github/raw107/src/Core/Models/Relics/` (read-only) |

`BaseYgoRelic.AssetProfile` resolves `IconPath`, `IconOutlinePath`, and `BigIconPath` from the runtime class name. Do not override it for a conventionally named icon.

## Naming

Use the supplied English name when present. Otherwise translate the Chinese title into a short noun phrase suitable for an item name.

1. Remove punctuation that cannot appear in a C# identifier.
2. Convert words to PascalCase.
3. Append `Relic` once.
4. Convert the full class name to uppercase snake case for localization and prefix it with `V_YGO_RELIC_`.

Examples:

| Chinese | English | Class | Localization prefix |
|---|---|---|---|
| 电子核心 | Cyber Core | `CyberCoreRelic` | `V_YGO_RELIC_CYBER_CORE_RELIC` |
| 决斗盘 | Duel Disk | `DuelDiskRelic` | `V_YGO_RELIC_DUEL_DISK_RELIC` |

Check for collisions across class files, localization keys, and PNG filenames before creating anything.

## Starter template

```csharp
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Relics.Starters;

[RegisterRelic(typeof(ZaneTruesdaleRelicPool))]
[RegisterCharacterStarterRelic(typeof(ZaneTruesdaleCharacter))]
public class ExampleRelic : BaseYgoRelic
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    public override RelicRarity Rarity => RelicRarity.Starter;

    // Add verified lifecycle hooks for the effect.
}
```

For non-starter relics, omit `RegisterCharacterStarterRelic`, select the requested `RelicRarity`, and use a namespace matching the chosen path.

## Effect implementation

Start from an original relic with similar timing:

- Combat start: inspect relics overriding `BeforeCombatStart`.
- Card play: inspect `AfterCardPlayed` and verify owner/card checks.
- Damage received: inspect `AfterDamageReceived` and distinguish blocked from unblocked damage.
- Turn energy: inspect `AfterEnergyReset`, `AfterSideTurnEnd`, and turn-number checks.
- Opening hand: inspect `ModifyHandDraw` rather than drawing later when the effect changes initial hand size.
- Per-combat limits: store mutable state and reset it in `AfterCombatEnd`.

Use the exact signatures in the installed STS2 version. Add only the `using` directives needed by the chosen implementation.

State rules:

- Guard activation with `Owner` checks in multiplayer-sensitive callbacks.
- Guard combat-only behavior with `CombatManager.Instance.IsInProgress` when the hook may run outside combat.
- Call `Flash()` immediately before the visible payoff.
- Wrap mutable fields in properties whose setter calls `AssertMutable()` when following model-state patterns.
- Reset state at the same scope as the limit: turn, combat, room, or run.
- Take `ToList()` snapshots before removing cards, minions, relics, or other members from an enumerated collection.

## DynamicVars and localization

Use a typed DynamicVar when one exists:

| Meaning | Typical type | Typical localization form |
|---|---|---|
| Energy | `EnergyVar` | `{Energy:energyIcons()}` |
| Cards | `CardsVar` | `{Cards}` |
| Block | `BlockVar` | `{Block}` |
| Damage | `DamageVar` | `{Damage}` |
| Healing | `HealVar` | `{Heal}` |
| Max HP | `MaxHpVar` | `{MaxHp}` |
| Power stacks | `PowerVar<TPower>` | Verify the generated variable name before writing localization |
| Custom amount | `new DynamicVar("Name", value)` | `{Name}` |

Confirm the actual property name through `DynamicVars` or a vanilla example. Do not assume a placeholder when uncertain.

Localization template:

```json
"V_YGO_RELIC_EXAMPLE_RELIC.title": "示例遗物",
"V_YGO_RELIC_EXAMPLE_RELIC.description": "效果描述，数值为{Energy:energyIcons()}。",
"V_YGO_RELIC_EXAMPLE_RELIC.flavor": "一句简短的风味文字。"
```

Keep the implementation and localized timing exact. Distinguish phrases such as “每回合首次”、“每场战斗首次”、“战斗开始时”和“获得时”.

## Review checklist

- The class name ends in `Relic` once.
- The file and icon use the exact class name and case.
- Registration points to the intended pool.
- Starter registration exists only for starter content.
- `Rarity` matches the design.
- Every localized number comes from the matching concrete `CanonicalVars` entry where practical.
- The effect checks owner, combat state, and trigger limit correctly.
- Temporary state resets at the intended boundary.
- Title, description, flavor, code, and icon all share one localization/class identity.
- The JSON parses and the project builds.
