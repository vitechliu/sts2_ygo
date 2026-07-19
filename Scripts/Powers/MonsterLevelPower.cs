using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public class MonsterLevelPower : ModPowerTemplate {
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/powers/ygo.png",
        BigIconPath: "res://VYgo/images/powers/ygo.png"
    );

    public static async Task SetLevel(
        PlayerChoiceContext choiceContext,
        Creature target,
        int level,
        Creature? applier,
        CardModel? cardSource) {
        MonsterLevelPower? power = target.GetPower<MonsterLevelPower>();
        if (power == null) {
            await PowerCmd.Apply<MonsterLevelPower>(
                choiceContext,
                target,
                level,
                applier,
                cardSource);
            return;
        }

        int offset = level - power.Amount;
        if (offset != 0) {
            await PowerCmd.ModifyAmount(choiceContext, power, offset, applier, cardSource);
        }
        RefreshYgoInfo(target);
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource) {
        RefreshYgoInfo(Owner);
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner) {
        RefreshYgoInfo(oldOwner);
        return Task.CompletedTask;
    }

    private static void RefreshYgoInfo(Creature creature) {
        creature.GetPower<YgoPower>()?.InitInfo();
    }
}
