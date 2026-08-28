using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Combat.Powers;
using STS2RitsuLib.Scaffolding.Content;

namespace VYgo.Scripts.Powers;

/// <summary>
/// 临时攻击力Power
/// </summary>
public abstract class TemporaryAttackPower<T> : ModTemporaryAppliedPowerTemplate<T, AttackPower>
    where T : AbstractModel
{
    // 类型，Buff或Debuff
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://images/powers/setup_strike_power.png",
        BigIconPath: "res://images/powers/setup_strike_power.png"
    );
    public override LocString Description => new("powers", "V_YGO_POWER_TEMPORARY_ATTACK_POWER.description");

    public override Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants
    ) {
        List<Creature> effectiveParticipants = participants.ToList();
        if (Owner.PetOwner is { } petOwner
            && effectiveParticipants.Contains(petOwner.Creature)) {
            effectiveParticipants.Add(Owner);
        }

        return base.AfterSideTurnEnd(choiceContext, side, effectiveParticipants);
    }
}
