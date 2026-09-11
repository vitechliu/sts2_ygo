using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Utils;
namespace VYgo.Scripts.Monsters.YGO;
public class CyberseGadgetMinion : BaseMonster {
    public override int CardId => 645087;
    public override async Task OnSummonYgo(PlayerChoiceContext choiceContext, Player owner, MinionSummonOptions options) {
        if (owner.MinionCount() >= owner.GetMaxMinionCount()) return;
        var selected = (await CardSelectCmd.FromCombatPile(choiceContext, PileType.Discard.GetPile(owner), owner,
            new CardSelectorPrefs(SourceCard!.SelectionScreenPrompt, 1),
            card => card is BaseMonsterCard { Level: > 0 and <= 2 })).FirstOrDefault();
        if (selected?.Pile?.Type == PileType.Discard && owner.MinionCount() < owner.GetMaxMinionCount())
            await CardCmd.AutoPlay(choiceContext, selected, null);
    }
    protected override async Task OnSendToGraveyard(PlayerChoiceContext choiceContext, Creature creature, Player owner) {
        if (SourceCard is not CyberseGadget source) return;
        if (!source.IsUpgraded) await CardCmd.Exhaust(choiceContext, source);
        if (owner.MinionCount() >= owner.GetMaxMinionCount() || owner.Creature.CombatState is not { } combat) return;
        var token = combat.CreateCard<CyberseGadgetToken>(owner);
        await CardPileCmd.AddGeneratedCardToCombat(token, PileType.Play, owner);
        await CardCmd.AutoPlay(choiceContext, token, null);
    }
}
