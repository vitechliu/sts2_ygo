using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Scripts.Cards.Category.Common;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public sealed class MirrorForcePower : ModPowerTemplate {
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/cards/44095762.png",
        BigIconPath: "res://VYgo/images/cards/44095762.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<MirrorForce>()
    ];

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay
    ) {
        return target == Owner && amount > 0m
            ? 0m
            : 1m;
    }

    public override async Task BeforeDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
    ) {
        if (target != Owner || amount <= 0m) return;

        int reflectedDamage = Amount;
        Flash();
        await PowerCmd.Remove(this);
        if (dealer == null) return;

        await CreatureCmd.Damage(
            choiceContext,
            dealer,
            reflectedDamage,
            ValueProp.Unpowered | ValueProp.SkipHurtAnim,
            Owner,
            null,
            null);
    }
}
