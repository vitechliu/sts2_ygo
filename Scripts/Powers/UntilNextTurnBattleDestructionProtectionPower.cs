using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public sealed class UntilNextTurnBattleDestructionProtectionPower : ModPowerTemplate {
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/powers/ygo.png",
        BigIconPath: "res://VYgo/images/powers/ygo.png");

    public override bool ShouldDie(Creature creature) => creature != Owner;

    public override Task AfterPreventingDeath(Creature creature) {
        return CreatureCmd.Heal(creature, 1m);
    }

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState) {
        // 额外玩家回合的参与者只包含玩家本体，随从仍应随其持有者回合到期。
        if (side == Owner.Side && (participants.Contains(Owner)
            || Owner.PetOwner is { } player && participants.Contains(player.Creature))) {
            await PowerCmd.Remove(this);
        }
    }
}
