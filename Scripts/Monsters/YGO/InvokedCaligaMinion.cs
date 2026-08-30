using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Monsters.YGO;

public class InvokedCaligaMinion : BaseMonster {
    public override int CardId => 13529466;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        await PowerCmd.Apply<InvokedCaligaPower>(
            choiceContext,
            Creature,
            1m,
            owner.Creature,
            options.Source,
            true);
    }
}
