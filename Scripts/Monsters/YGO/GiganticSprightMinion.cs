using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Minion;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Common;
using VYgo.Scripts.Powers;
using VYgo.Utils;

namespace VYgo.Scripts.Monsters.YGO;

public class GiganticSprightMinion: BaseMonster {
    public override int CardId => 54498517;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not GiganticSpright sourceCard
            || owner.MinionCount() >= MinionUtil.MaxMinionCount) {
            return;
        }

        BaseMonsterCard? selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                PileType.Draw.GetPile(owner),
                owner,
                new CardSelectorPrefs(sourceCard.SelectionScreenPrompt, 1),
                IsLevel2Monster))
            .OfType<BaseMonsterCard>()
            .FirstOrDefault();
        if (selected == null || owner.MinionCount() >= MinionUtil.MaxMinionCount) return;

        await selected.AutoPlayAndCaptureSummonedCreature(choiceContext, null);
    }

    // 超量素材中有融合·同调·超量·连接怪兽时，这张卡攻击力翻倍
    public override async Task OnXyzMaterialsAttached(
        PlayerChoiceContext choiceContext,
        Player owner,
        IReadOnlyList<CardModel> materials) {
        bool hasExtraDeckType = materials.Any(card =>
            (card as IYgoId)?.YgoGetCore() is { Types: { } types }
            && (types.Contains("融合")
                || types.Contains("同调")
                || types.Contains("超量")
                || types.Contains("连接")));
        if (!hasExtraDeckType) return;

        decimal attack = Creature.GetPowerAmount<AttackPower>();
        if (attack <= 0m) return;
        await PowerCmd.Apply<AttackPower>(choiceContext, Creature, attack, owner.Creature, SourceCard);
    }

    private static bool IsLevel2Monster(CardModel card) {
        return card is BaseMonsterCard monster
            && !monster.IsExtra
            && YgoSummonRules.IsLevel2OrRank2(monster.YgoGetCore());
    }
}
