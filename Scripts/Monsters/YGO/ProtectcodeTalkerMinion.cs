using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Utils;
namespace VYgo.Scripts.Monsters.YGO;
public class ProtectcodeTalkerMinion : BaseMonster {
    public override int CardId => 58036229;
    public override async Task OnSummonYgo(PlayerChoiceContext choiceContext, Player owner, MinionSummonOptions options) {
        if (options.Source is not ProtectcodeTalker card) return;
        foreach (var pet in owner.Creature.Pets.Where(pet => pet.IsAlive && pet.Monster is BaseMonster { SourceCard: BaseExtraLinkCard }).ToList()) {
            int links = ((BaseMonster)pet.Monster!).YgoGetCore()?.LinkCount ?? 0;
            await MinionUtil.AddHp(pet, links * card.DynamicVars["Boost"].IntValue);
        }
    }
}
