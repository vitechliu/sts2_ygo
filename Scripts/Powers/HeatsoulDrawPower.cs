using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public sealed class HeatsoulDrawPower : ModPowerTemplate {
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/powers/heatsoul_draw_power.png",
        BigIconPath: "res://VYgo/images/powers/heatsoul_draw_power.png");

    public override async Task AfterPlayerTurnStart(
        MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext choiceContext, Player player) {
        if (player == Owner.PetOwner && Owner.IsAlive) {
            await MegaCrit.Sts2.Core.Commands.CardPileCmd.Draw(choiceContext, Amount, player);
        }
    }
}
