using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace VYgo.Core;

public interface IEquipmentEffect {
    Task OnEquipped(
        PlayerChoiceContext choiceContext,
        Creature target);

    Task OnUnequipped(
        PlayerChoiceContext choiceContext,
        Creature target);
}
