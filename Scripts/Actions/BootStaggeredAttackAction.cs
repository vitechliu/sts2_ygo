using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Scripts.Monsters.YGO;
using VYgo.Utils;

namespace VYgo.Scripts.Actions;

public class BootStaggeredAttackAction : TargetingAttackAction {
    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target) {
        if (target == null) return;

        await base.OnAct(choiceContext, target);
    
        if (Owner.Monster is not BootStaggeredMinion monster) {
            Entry.Logger.Error("Cannot Find BootStaggeredMinion");
            return;
        }

        if (Owner.PetOwner != null && Owner.PetOwner.MinionCount() < Owner.PetOwner.GetMaxMinionCount()) {
            CardModel token = CombatState.CreateCard<BootStaggeredToken>(Owner.PetOwner);
            await CardPileCmd.AddGeneratedCardToCombat(
                token,
                PileType.Play,
                Owner.PetOwner
            );
            await CardCmd.AutoPlay(choiceContext, token, null);
        }
    }
}
