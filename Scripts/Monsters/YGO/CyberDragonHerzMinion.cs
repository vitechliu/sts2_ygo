using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Scripts.Cards;

namespace VYgo.Scripts.Monsters.YGO;

public class CyberDragonHerzMinion: BaseMonster {
    public override int CardId => 56364287;

    protected override async Task OnSendToGraveyard(PlayerChoiceContext choiceContext, Creature creature, Player owner) {
        CardPile pile = PileType.Draw.GetPile(owner);
        IEnumerable<CardModel> source = pile.Cards.OfType<BaseMonsterCard>().Where((BaseMonsterCard c) => !c.IsExtra && c.ArchetypesList.Contains(YgoArchetypes.CyberDragon));
        IEnumerable<CardModel> enumerable = source.ToList().UnstableShuffle(owner.RunState.Rng.CombatCardSelection).Take(_upgraded ? 2 : 1);
        foreach (CardModel card in enumerable) {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }
}
