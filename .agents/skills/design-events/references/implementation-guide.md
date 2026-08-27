# Event implementation guide

## File and identity conventions

Use these project paths:

```text
Scripts/Events/<EventClass>.cs
VYgo/localization/zhs/events.json
VYgo/images/events/<event_stem>.png
```

RitsuLib's default public entry combines mod ID, model category, and type name:

```text
BloodBargain -> V_YGO_EVENT_BLOOD_BARGAIN
```

Prefer an explicit event asset path instead of relying on the base game's synthesized path:

```csharp
public override EventAssetProfile AssetProfile => new(
    InitialPortraitPath: "res://VYgo/images/events/blood_bargain.png"
);
```

The default event layout is sufficient for ordinary narrative events. Add a custom layout, background scene, or VFX only when the specification needs behavior the default portrait layout cannot express.

## Registration

These concepts are separate:

```csharp
// Included in every act's shared content pool.
[RegisterSharedEvent]

// Included only in these act models.
[RegisterActEvent(typeof(Overgrowth))]
[RegisterActEvent(typeof(Underdocks))]

// Multiplayer players vote on one choice; unrelated to registration scope.
public override bool IsShared => true;
```

Known base-game act models:

- `Overgrowth`: default act 1 route.
- `Underdocks`: alternate act 1 route.
- `Hive`: act 2.
- `Glory`: act 3.

Personal reward events should normally retain `IsShared == false`, allowing multiplayer players to choose independently.

## Minimal event pattern

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace VYgo.Scripts.Events;

[RegisterSharedEvent]
public sealed class BloodBargain : ModEventTemplate
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(12m),
        new GoldVar(75m),
    ];

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://VYgo/images/events/blood_bargain.png"
    );

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new EventOption(
                this,
                TakeRelic,
                InitialOptionKey("TAKE_RELIC"),
                HoverTipFactory.FromRelic<MyEventRelic>())
            .ThatDoesDamage(DynamicVars.HpLoss.BaseValue),
        new EventOption(this, TakeGold, InitialOptionKey("TAKE_GOLD")),
    ];

    private async Task TakeRelic()
    {
        var owner = Owner ?? throw new InvalidOperationException("Event owner is unavailable.");
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            owner.Creature,
            DynamicVars.HpLoss.BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null,
            null);
        await RelicCmd.Obtain<MyEventRelic>(owner);
        SetEventFinished(PageDescription("RELIC_TAKEN"));
    }

    private async Task TakeGold()
    {
        var owner = Owner ?? throw new InvalidOperationException("Event owner is unavailable.");
        await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, owner);
        SetEventFinished(PageDescription("GOLD_TAKEN"));
    }
}
```

`ThatDoesDamage` only supplies lethal-choice warning behavior. The callback must still invoke `CreatureCmd.Damage`.

For percentage or randomized values, calculate the DynamicVar before options are created:

```csharp
public override void CalculateVars()
{
    DynamicVars.Gold.BaseValue = Rng.NextInt(60, 91);
}
```

Use the event-local `Rng` for event randomness. Confirm the exact `NextInt` bounds from current source before promising inclusive or exclusive behavior.

## Relic rewards

For a fixed relic, register it in `ModelDb` first and perform the actual obtain command in the callback:

```csharp
await RelicCmd.Obtain<MyEventRelic>(owner);
```

For a random relic:

```csharp
var relic = RelicFactory.PullNextRelicFromFront(owner).ToMutable();
await RelicCmd.Obtain(relic, owner);
```

`PullNextRelicFromFront` consumes the reward from the grab bag. Call it only when that consumption is intended. If the event must reveal a specific random relic before choice, explicitly design when it is reserved and how cancellation or alternate choices affect the bag.

## Availability and locked options

Use `IsAllowed(IRunState runState)` for conditions that decide whether the entire event can enter the act's event sequence. `Owner` is unavailable there.

Create a locked option by passing a null callback only when the event should remain available but that choice is unavailable:

```csharp
new EventOption(this, null, InitialOptionKey("LOCKED"))
```

Prefer a locked localized explanation over a clickable callback that silently does nothing.

## Localization contract

Add all keys to `VYgo/localization/zhs/events.json`:

```json
{
  "V_YGO_EVENT_BLOOD_BARGAIN.title": "血之交易",
  "V_YGO_EVENT_BLOOD_BARGAIN.pages.INITIAL.description": "……",
  "V_YGO_EVENT_BLOOD_BARGAIN.pages.INITIAL.options.TAKE_RELIC.title": "献上生命",
  "V_YGO_EVENT_BLOOD_BARGAIN.pages.INITIAL.options.TAKE_RELIC.description": "失去[red]{HpLoss}[/red]点生命。获得一件[gold]遗物[/gold]。",
  "V_YGO_EVENT_BLOOD_BARGAIN.pages.INITIAL.options.TAKE_GOLD.title": "拿走金币",
  "V_YGO_EVENT_BLOOD_BARGAIN.pages.INITIAL.options.TAKE_GOLD.description": "获得[blue]{Gold}[/blue][gold]金币[/gold]。",
  "V_YGO_EVENT_BLOOD_BARGAIN.pages.RELIC_TAKEN.description": "……",
  "V_YGO_EVENT_BLOOD_BARGAIN.pages.GOLD_TAKEN.description": "……"
}
```

The event UI adds DynamicVars to option titles and descriptions. Use the same variable names as the implementation. Preserve option and page tokens after release because they are used by localization and run-history choice records.

Polish the Chinese text by role:

- opening description: establish place, sensory detail, and the immediate dilemma in two to four compact paragraphs;
- option title: use a short verb-led action, not a full mechanical sentence;
- option description: state cost, condition, target, amount, and reward exactly in execution order;
- result page: describe the consequence in one to three sentences without repeating the full option tooltip;
- rich text: use established tags such as `[red]`, `[gold]`, and `[blue]` only where they improve scanning.

Keep exact values in DynamicVars instead of duplicating localized literals. Do not fabricate partial English gameplay localization unless explicitly requested.

## Source research targets

Search current sources for patterns rather than relying on this example alone:

```text
${STS2_VANILLA_ROOT}/src/Core/Models/EventModel.cs
${STS2_VANILLA_ROOT}/src/Core/Events/EventOption.cs
${STS2_VANILLA_ROOT}/src/Core/Models/Events/
${RITSULIB_ROOT}/src/Scaffolding/Content/ModEventTemplate.cs
${RITSULIB_ROOT}/docs/pages/guide/custom-events.md
```

Useful search concepts include `RelicCmd.Obtain`, `PlayerCmd.GainGold`, `CreatureCmd.Damage`, `SetEventFinished`, `SetEventState`, `ThatDoesDamage`, `IsAllowed`, and the exact reward or selection command needed by the option.
