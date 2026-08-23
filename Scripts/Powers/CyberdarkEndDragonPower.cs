using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core;
using VYgo.Scripts.Cards;

namespace VYgo.Scripts.Powers;

/// <summary>
/// 铠皇龙-电子暗黑终结龙：你的回合结束时，从弃牌堆选择一只怪兽装备给此怪兽。
/// </summary>
[RegisterPower]
public class CyberdarkEndDragonPower : ModPowerTemplate {
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/powers/equipment_power.png",
        BigIconPath: "res://VYgo/images/powers/equipment_power.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Equip()
    ];

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants) {
        if (side != Owner.Side
            || !participants.Contains(Owner)
            || !Owner.IsAlive
            || Owner.PetOwner is not { } player) {
            return;
        }

        CardModel? selectedMonster = (await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                context: choiceContext,
                pile: PileType.Discard.GetPile(player),
                player: player,
                filter: card => card is BaseMonsterCard))
            .FirstOrDefault();
        if (selectedMonster == null) return;

        Flash();
        await EquipCmd.EquipFromPile(choiceContext, selectedMonster, Owner);
    }
}
