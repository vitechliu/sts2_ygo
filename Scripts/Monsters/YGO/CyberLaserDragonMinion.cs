using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;

namespace VYgo.Scripts.Monsters.YGO;

public class CyberLaserDragonMinion: BaseMonster {
    public override int CardId => 4162088;

    public override bool BasicAttackAction => false;

    public decimal VulnerableAmount => SourceCard?.DynamicVars.Vulnerable.BaseValue ?? 2m;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options) {
        if (options.PrimaryStatAmount is { } strength && strength > 0m) {
            await ApplyMonsterAction<CyberLaserAttackAction>(
                choiceContext,
                Creature,
                strength,
                owner.Creature,
                options.Source,
                true);
        }
    }
}
