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
    private sealed class Data {
        public PlayerChoiceContext? ChoiceContext { get; set; }
        public Creature? Dealer { get; set; }
        public bool PreventedDamage { get; set; }
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/powers/mirror_force_power.png",
        BigIconPath: "res://VYgo/images/powers/mirror_force_power.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<MirrorForce>()
    ];

    protected override object InitInternalData() => new Data();

    public override Task BeforeDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
    ) {
        Data data = GetInternalData<Data>();
        data.ChoiceContext = target == Owner && amount > 0m ? choiceContext : null;
        data.Dealer = target == Owner && amount > 0m ? dealer : null;
        data.PreventedDamage = false;
        return Task.CompletedTask;
    }

    public override decimal ModifyHpLostAfterOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
    ) {
        Data data = GetInternalData<Data>();
        if (target != Owner || amount <= 0m || data.ChoiceContext == null) {
            return amount;
        }

        data.PreventedDamage = true;
        return 0m;
    }

    public override async Task AfterModifyingHpLostAfterOsty() {
        Data data = GetInternalData<Data>();
        if (!data.PreventedDamage || data.ChoiceContext == null) return;

        PlayerChoiceContext choiceContext = data.ChoiceContext;
        Creature? dealer = data.Dealer;
        int reflectedDamage = Amount;
        data.ChoiceContext = null;
        data.Dealer = null;
        data.PreventedDamage = false;

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
