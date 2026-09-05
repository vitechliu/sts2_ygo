using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public sealed class FiendsmithEngraverUsedThisTurnPower : ModPowerTemplate {
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/powers/fiendsmith_engraver_used_this_turn_power.png",
        BigIconPath: "res://VYgo/images/powers/fiendsmith_engraver_used_this_turn_power.png"
    );

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants
    ) {
        if (participants.Contains(Owner)) {
            await PowerCmd.Remove(this);
        }
    }
}
