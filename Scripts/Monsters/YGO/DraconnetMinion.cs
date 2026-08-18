using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Minion;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Scripts.Powers;
using VYgo.Utils;

namespace VYgo.Scripts.Monsters.YGO;

public class DraconnetMinion: BaseMonster {
    public override int CardId => 62706865;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not Draconnet sourceCard
            || owner.MinionCount() >= MinionUtil.MaxMinionCount) {
            return;
        }

        BaseMonsterCard? selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                PileType.Draw.GetPile(owner),
                owner,
                new CardSelectorPrefs(sourceCard.SelectionScreenPrompt, 1),
                IsLowLevelNormalMonster))
            .OfType<BaseMonsterCard>()
            .FirstOrDefault();
        if (selected == null || owner.MinionCount() >= MinionUtil.MaxMinionCount) return;

        if (sourceCard.IsUpgraded) {
            CardCmd.Upgrade(selected);
        }

        Creature? summoned = await selected.AutoPlayAndCaptureSummonedCreature(
            choiceContext,
            null);
        if (summoned != null) {
            await PowerCmd.Apply<MonsterActionLockedThisTurnPower>(
                choiceContext,
                summoned,
                1m,
                Creature,
                sourceCard,
                true);
        }
    }

    private static bool IsLowLevelNormalMonster(CardModel card) {
        return card is BaseMonsterCard monster
            && !monster.IsExtra
            && monster.YgoGetCore() is { HasLevel: true, Level: <= 2 } core
            && !core.IsEffectMonster;
    }
}
