using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Minion;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;

namespace VYgo.Scripts.Monsters.YGO;

public class CyberdarkChimeraMinion: BaseMonster {
    public override int CardId => 5370235;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options) {
        CardModel powerBond = owner.Creature.CombatState.CreateCard<PowerBond>(owner);
        if (options.Source is CyberdarkChimera { IsUpgraded: true }) {
            CardCmd.Upgrade(powerBond);
        }

        await CardPileCmd.AddGeneratedCardToCombat(powerBond, PileType.Hand, owner);
    }

    protected override async Task OnSendToGraveyard(
        PlayerChoiceContext choiceContext,
        Creature creature,
        Player owner) {
        BaseMonsterCard? template = owner.RunState.Rng.CombatCardSelection.NextItem(
            ModelDb.AllCards
                .OfType<BaseMonsterCard>()
                .Where(card => card.ContainArchetype(YgoArchetypes.Cyberdark))
                .ToList());
        if (template == null) return;

        CardModel generatedMonster = owner.Creature.CombatState.CreateCard(template, owner);
        if (_upgraded) {
            CardCmd.Upgrade(generatedMonster);
        }

        await CardPileCmd.AddGeneratedCardToCombat(
            generatedMonster,
            PileType.Discard,
            owner);
    }
}
