using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace VYgo.Scripts.Powers;

/// <summary>
/// 在当前攻击结算期间防止怪兽被战斗破坏。
/// </summary>
[RegisterPower]
public sealed class BattleDestructionProtectionPower : ModPowerTemplate {
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/powers/ygo.png",
        BigIconPath: "res://VYgo/images/powers/ygo.png"
    );

    public override bool ShouldDie(Creature creature) => creature != Owner;

    public override async Task AfterPreventingDeath(Creature creature) {
        await CreatureCmd.Heal(creature, 1m);
        await PowerCmd.Remove(this);
    }

    public override async Task AfterAttack(
        PlayerChoiceContext choiceContext,
        AttackCommand command
    ) {
        if (Owner.IsAlive) {
            await PowerCmd.Remove(this);
        }
    }
}
