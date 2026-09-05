using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Cards.Category.Common;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public sealed class ForbiddenChaliceStrengthPower : TemporaryStrengthPower {
    public override AbstractModel OriginModel => ModelDb.Card<ForbiddenChalice>();

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants
    ) {
        if (side != CombatSide.Player) return;

        Flash();
        int amount = Amount;
        await PowerCmd.Remove(this);
        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner,
            -amount,
            Owner,
            null);
    }
}

[RegisterPower]
public sealed class ForbiddenLanceStrengthPower : TemporaryStrengthPower {
    public override AbstractModel OriginModel => ModelDb.Card<ForbiddenLance>();
    protected override bool IsPositive => false;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants
    ) {
        if (side != CombatSide.Player) return;

        Flash();
        int amount = Amount;
        await PowerCmd.Remove(this);
        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner,
            amount,
            Owner,
            null);
    }
}
