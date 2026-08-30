using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Monsters.YGO;

public class CyberdarkEndDragonMinion : BaseMonster {
    public override int CardId => 37542782;

    public override bool BasicAttackAction => false;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.PrimaryStatAmount is { } strength && strength > 0m)
            await ApplyMonsterAction<CyberdarkEndDragonAttackAction>(
                choiceContext,
                Creature,
                1m,
                owner.Creature,
                options.Source,
                true);

        await PowerCmd.Apply<CyberdarkEndDragonPower>(
            choiceContext,
            Creature,
            1m,
            owner.Creature,
            options.Source,
            true);
    }
}
