using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Utils;

namespace VYgo.Scripts.Monsters.YGO;

// 类名不能以大写缩写开头：未显式注册的模型 ID 由游戏 Slugify 生成，
// "ROMCloudiaMinion" 会变成 R_OM_CLOUDIA_MINION，与本地化键 ROM_CLOUDIA_MINION 不匹配。
public class RomCloudiaMinion: BaseMonster {
    public override int CardId => 44956694;

    protected override async Task OnSendToGraveyard(
        PlayerChoiceContext choiceContext,
        Creature creature,
        Player owner
    ) {
        if (SourceCard is not ROMCloudia { IsUpgraded: true } sourceCard
            || owner.MinionCount() >= owner.GetMaxMinionCount()) {
            return;
        }

        CardModel? selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                PileType.Draw.GetPile(owner),
                owner,
                new CardSelectorPrefs(
                    new LocString("cards", "V_YGO_CARD_ROM_CLOUDIA.graveyardSelectionScreenPrompt"),
                    1),
                IsLowLevelCyberseMonster))
            .FirstOrDefault();
        if (selected != null && owner.MinionCount() < owner.GetMaxMinionCount()) {
            await CardCmd.AutoPlay(choiceContext, selected, null);
        }
    }

    private static bool IsLowLevelCyberseMonster(CardModel card) {
        return card is BaseMonsterCard monster
            && !monster.IsExtra
            && monster.Level is > 0 and <= 4
            && monster.YgoGetCore().IsRace(YgoRace.Cyberse);
    }
}
