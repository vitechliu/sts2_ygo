using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace VYgo.Scripts.Powers;

/// <summary>
/// 减少敌人获得的强化，或敌人对同一玩家控制的角色与怪兽施加的负面效果；
/// 每抵消一层效果，消耗一层无效。
/// </summary>
[RegisterPower]
public class NegatingPower : ModPowerTemplate
{
    private sealed class Data
    {
        public readonly Stack<int> PendingNegatedAmounts = new();

        public int ReservedAmount { get; set; }
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => true;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/powers/negating_power.png",
        BigIconPath: "res://VYgo/images/powers/negating_power.png"
    );

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (amount == 0m || !canonicalPower.IsVisible)
        {
            return false;
        }

        PowerType incomingType = canonicalPower.GetTypeForAmount(amount);
        bool isEnemyBuff =
            amount > 0m &&
            target.Side != Owner.Side &&
            incomingType == PowerType.Buff;
        bool isEnemyDebuffOnProtectedTarget =
            IsControlledBySamePlayer(target) &&
            applier != null &&
            applier.Side != Owner.Side &&
            incomingType == PowerType.Debuff;

        if (!isEnemyBuff && !isEnemyDebuffOnProtectedTarget)
        {
            return false;
        }

        Data data = GetInternalData<Data>();
        int availableAmount = Amount - data.ReservedAmount;
        int negatedAmount = Math.Min(availableAmount, (int)amount);
        if (negatedAmount <= 0)
        {
            return false;
        }

        data.PendingNegatedAmounts.Push(negatedAmount);
        data.ReservedAmount += negatedAmount;
        modifiedAmount = amount - negatedAmount;
        return true;
    }

    private bool IsControlledBySamePlayer(Creature target)
    {
        if (target == Owner)
        {
            return true;
        }

        var controller = Owner.Player ?? Owner.PetOwner;
        return controller != null &&
               (target.Player == controller || target.PetOwner == controller);
    }

    public override async Task AfterModifyingPowerAmountReceived(PowerModel power)
    {
        Data data = GetInternalData<Data>();
        int negatedAmount = data.PendingNegatedAmounts.Pop();
        data.ReservedAmount -= negatedAmount;
        await PowerCmd.ModifyAmount(
            new ThrowingPlayerChoiceContext(),
            this,
            -negatedAmount,
            null,
            null);
    }
}
