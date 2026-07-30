using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public class EquipmentPower : ModPowerTemplate {
    private CardModel? _equipmentCard;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public CardModel? EquipmentCard => _equipmentCard;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/powers/equipment_power.png",
        BigIconPath: "res://VYgo/images/powers/equipment_power.png"
    );

    public override LocString Title =>
        _equipmentCard?.TitleLocString ?? base.Title;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        _equipmentCard == null
            ? []
            : [HoverTipFactory.FromCard(_equipmentCard)];

    internal bool Initialize(CardModel card) {
        AssertMutable();
        if (_equipmentCard != null) return false;

        _equipmentCard = card;
        return true;
    }

    internal CardModel? TakeEquipmentCard() {
        AssertMutable();
        CardModel? card = _equipmentCard;
        _equipmentCard = null;
        return card;
    }

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength) {
        if (creature != Owner || wasRemovalPrevented) return;

        await EquipCmd.ReleaseEquipmentPower(
            choiceContext,
            this,
            removePower: false,
            knownOwner: creature);
    }

    public override async Task AfterRemoved(Creature oldOwner) {
        if (_equipmentCard == null) return;

        await EquipCmd.ReleaseEquipmentPower(
            new ThrowingPlayerChoiceContext(),
            this,
            removePower: false,
            knownOwner: oldOwner);
    }
}
