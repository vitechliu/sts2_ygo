using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;

namespace VYgo.Scripts.Monsters.YGO;

// 类名不能以大写缩写开头：未显式注册的模型 ID 由游戏 Slugify 生成，
// "RAMClouderMinion" 会变成 R_AM_CLOUDER_MINION，与本地化键 RAM_CLOUDER_MINION 不匹配。
public class RamClouderMinion: BaseMonster {
    public override int CardId => 9190563;

    public override bool BasicAttackAction => false;

    public override Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        return ApplyMonsterAction<RamClouderAction>(
            choiceContext,
            Creature,
            1m,
            owner.Creature,
            options.Source,
            true);
    }
}
