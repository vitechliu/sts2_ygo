using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Core;
using VYgo.Scripts.Cards.Category.Link;

namespace VYgo.Scripts.Monsters.YGO;

// 类名不能以大写缩写开头：未显式注册的模型 ID 由游戏 Slugify 生成，
// "SPLittleKnightMinion" 会变成 S_P_LITTLE_KNIGHT_MINION，与本地化键 SP_LITTLE_KNIGHT_MINION 不匹配。
public class SpLittleKnightMinion: BaseMonster {
    public override int CardId => 29301450;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options) {
        if (options.Source is not SPLittleKnight sourceCard) return;

        var target = owner.RunState.Rng.CombatTargets.NextItem(
            Creature.CombatState.HittableEnemies);
        if (target != null) {
            await BanishCmd.Banish(target, sourceCard.BanishAmount);
        }
    }
}
