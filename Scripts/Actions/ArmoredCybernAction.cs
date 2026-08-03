using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MinionLib.Targeting;
using STS2RitsuLib.Ui.Toast;
using VYgo.Core;
using VYgo.Scripts.Monsters;

namespace VYgo.Scripts.Actions;

public sealed class ArmoredCybernAction : BasePerTurnMonsterAction {
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Action(),
        YgoHoverTipConst.Equip(),
        YgoHoverTipConst.Enhance()
    ];

    protected override bool IsVisibleInternal => true;

    public override TargetType TargetType => MinionTargetTypes.AnyMinion;

    protected override string? IntentIconPath => "res://VYgo/images/powers/equipment_power.png";
    public override string? CustomIconPath => IntentIconPath;

    protected override async Task OnAct(
        PlayerChoiceContext choiceContext,
        Creature? target
    ) {
        if (!IsCyberDragonMonster(target)) {
            ShowInvalidTargetToast();
            return;
        }

        if (Owner.Monster is not BaseMonster armoredCybern
            || armoredCybern.SourceCard is not { } sourceCard
            || !armoredCybern.TryReserveSourceCardAsSummonMaterial(sourceCard)) {
            return;
        }

        if (!await EquipCmd.EquipFromPile(choiceContext, sourceCard, target)) {
            armoredCybern.CancelSourceCardMaterialReservation(sourceCard);
            return;
        }

        SpendUses();
        await CreatureCmd.Kill(Owner, true);
    }

    private static bool IsCyberDragonMonster(Creature? target) {
        return target?.Monster is BaseMonster monster
            && monster.YgoGetCard()?.ContainArchetype(YgoArchetypes.CyberDragon) == true;
    }

    private static void ShowInvalidTargetToast() {
        RitsuToastService.ShowWarning(
            new LocString("powers", "ARMORED_CYBERN_ACTION.targetError").GetFormattedText(),
            new LocString("powers", "ARMORED_CYBERN_ACTION.targetErrorTitle").GetFormattedText()
        );
    }
}

