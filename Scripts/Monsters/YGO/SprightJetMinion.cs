using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Minion;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Common;

namespace VYgo.Scripts.Monsters.YGO;

public class SprightJetMinion: BaseMonster {
    public override int CardId => 13533678;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not SprightJet sourceCard) return;
        // 特召登场才触发检索
        if (sourceCard.LastSummonContext?.IsSpecialSummon != true) return;

        CardPile pile = PileType.Draw.GetPile(owner);
        IEnumerable<CardModel> source = pile.Cards
            .OfType<BaseVYgoCard>()
            .Where(c => (c is BaseSpellCard or BaseTrapCard) && c.ContainArchetype(YgoArchetypes.Spright));
        IEnumerable<CardModel> picked = source.ToList()
            .UnstableShuffle(owner.RunState.Rng.CombatCardSelection)
            .Take(1);
        foreach (CardModel card in picked) {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }
}
