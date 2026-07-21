using MegaCrit.Sts2.Core.Entities.Players;

namespace VYgo.Core;

/// <summary>
/// Marks an extra-deck card that can start its summon flow directly from the extra pile UI.
/// </summary>
public interface IDirectExtraDeckSummonCard {
    DirectExtraDeckSummonSpec? CreateDirectExtraDeckSummonSpec(Player owner);
}

/// <summary>
/// Describes the summon behavior after the target extra-deck card has already been chosen.
/// </summary>
public sealed record DirectExtraDeckSummonSpec(
    Func<SummonMaterialSelectionSpec?> BuildMaterialSelection,
    Func<SummonAnimationContext, Task> PlayAnimation,
    Func<IReadOnlyList<SummonMaterial>, Task>? ConsumeMaterials = null,
    float FinalWaitSeconds = 0.45f
);
