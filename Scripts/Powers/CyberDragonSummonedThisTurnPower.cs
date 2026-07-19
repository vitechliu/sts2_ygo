using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public class CyberDragonSummonedThisTurnPower : ModPowerTemplate {
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override bool IsVisibleInternal => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/powers/ygo.png",
        BigIconPath: "res://VYgo/images/powers/ygo.png"
    );

    public override bool TryModifyEnergyCostInCombatLate(
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost) {
        modifiedCost = originalCost;
        if (card.Owner.Creature != Owner || card is not CyberDragonVier vier) {
            return false;
        }
        if (card.Pile?.Type is not (PileType.Hand or PileType.Play)) {
            return false;
        }

        modifiedCost = Math.Max(0m, originalCost - vier.DynamicVars["CostReduction"].BaseValue);
        return modifiedCost != originalCost;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants) {
        if (participants.Contains(Owner)) {
            await PowerCmd.Remove(this);
        }
    }
}
