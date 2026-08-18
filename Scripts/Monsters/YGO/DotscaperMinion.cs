using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using VYgo.Core;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Utils;

namespace VYgo.Scripts.Monsters.YGO;

public class DotscaperMinion: BaseMonster {
    public override int CardId => 18789533;

    protected override async Task OnSendToGraveyard(
        PlayerChoiceContext choiceContext,
        Creature creature,
        Player owner
    ) {
        if (SourceCard is not Dotscaper sourceCard
            || owner.MinionCount() >= MinionUtil.MaxMinionCount
            || !sourceCard.CanUseEffectByCard(
                creature.CombatState,
                owner,
                "graveyard")) {
            return;
        }

        await CardCmd.AutoPlay(choiceContext, sourceCard, null);
    }
}
