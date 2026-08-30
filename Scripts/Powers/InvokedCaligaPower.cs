using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Scripts.Monsters;

namespace VYgo.Scripts.Powers;

/// <summary>
/// 召唤兽 卡利古拉在场时，敌方攻击友方阵营后为友方角色施加缓冲。
/// </summary>
[RegisterPower]
public sealed class InvokedCaligaPower : ModPowerTemplate {
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://images/powers/colossus_power.png",
        BigIconPath: "res://images/powers/colossus_power.png"
    );

    public override async Task AfterAttack(
        PlayerChoiceContext choiceContext,
        AttackCommand command
    ) {
        if (!Owner.IsAlive
            || command.Attacker is not { } attacker
            || attacker.Side == Owner.Side
            || command.TargetSide != Owner.Side) {
            return;
        }

        List<Creature> friendlyCharacters = Owner.CombatState.Players
            .Select(player => player.Creature)
            .Where(creature => creature.IsAlive && creature.Side == Owner.Side)
            .ToList();
        if (friendlyCharacters.Count == 0) return;

        CardModel? sourceCard = (Owner.Monster as BaseMonster)?.SourceCard;
        Flash();
        await PowerCmd.Apply<InvokedCaligaBufferPower>(
            choiceContext,
            friendlyCharacters,
            Amount,
            Owner,
            sourceCard);
    }
}
