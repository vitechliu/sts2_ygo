using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.YgoEvent;

internal static class InvokedFusionUtil {
    public const int AleisterCardId = 86120751;

    public static bool IsInvokedFusionMonster(BaseExtraFusionCard card) {
        return card.ContainArchetype(YgoArchetypes.Invoked);
    }

    public static bool CanUseMaterial(SummonMaterial material, string attribute) {
        return material.CardId == AleisterCardId
            || material.CoreCard?.Attribute == attribute;
    }

    public static bool HasAleisterAndAttribute(
        IReadOnlyList<SummonMaterial> materials,
        string attribute
    ) {
        for (int aleisterIndex = 0; aleisterIndex < materials.Count; aleisterIndex++) {
            if (materials[aleisterIndex].CardId != AleisterCardId) continue;

            for (int attributeIndex = 0; attributeIndex < materials.Count; attributeIndex++) {
                if (attributeIndex == aleisterIndex) continue;
                if (materials[attributeIndex].CoreCard?.Attribute == attribute) return true;
            }
        }

        return false;
    }
}
