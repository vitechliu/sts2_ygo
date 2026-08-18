using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using VYgo.Core;
using VYgo.Scripts.Cards.Category.Playmaker;

namespace VYgo.Scripts.Monsters.YGO;

public class LatencyMinion: BaseMonster {
    public override int CardId => 3560069;

    public override async Task OnUsedAsLinkMaterial(
        PlayerChoiceContext choiceContext,
        Player owner,
        IReadOnlyList<SummonMaterial> materials
    ) {
        if (SourceCard is Latency sourceCard) {
            await CardPileCmd.Draw(choiceContext, sourceCard.Draw, owner);
        }
    }
}
