using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Scripts.Cards;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public class CyberneticHiddenTechnologyPower : ModPowerTemplate {
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/powers/cybernetic_hidden_technology_power.png",
        BigIconPath: "res://VYgo/images/powers/cybernetic_hidden_technology_power.png"
    );

    public override async Task AfterAttack(
        PlayerChoiceContext choiceContext,
        AttackCommand command) {
        if (command.Attacker is not { IsAlive: true } attacker
            || attacker.Side == Owner.Side
            || command.TargetSide != Owner.Side
            || Owner.Player is not { } player) {
            return;
        }

        CardPile extraPile = Entry.ExtraPile.GetPile(player);
        List<BaseExtraFusionCard> fusionMonsters = extraPile.Cards
            .OfType<BaseExtraFusionCard>()
            .ToList();
        BaseExtraFusionCard? randomFusion = player.RunState.Rng.CombatCardSelection
            .NextItem(fusionMonsters);
        if (randomFusion == null
            || randomFusion.Pile != extraPile
            || !extraPile.Cards.Contains(randomFusion)) {
            return;
        }

        Flash();
        await CardPileCmd.Add(randomFusion, PileType.Discard);
        if (attacker.IsAlive) {
            await CreatureCmd.Damage(
                choiceContext,
                attacker,
                Amount,
                ValueProp.Unpowered,
                Owner);
        }
    }
}
