using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Minion;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Monsters;

namespace VYgo.Scripts.Powers;

/// <summary>
/// 综合Power，用于展示BaseMonster所有信息
/// </summary>
[RegisterPower]
public class YgoPower : ModPowerTemplate, IYgoId {
    // 类型，Buff或Debuff
    public override PowerType Type => PowerType.Buff;

    // 叠加类型，Counter表示可叠加，Single表示不可叠加
    public override PowerStackType StackType => PowerStackType.Single;

    public BaseVYgoCard? Card { get; set; }

    public bool IsGuardian { get; set; }

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/powers/ygo.png",
        BigIconPath: "res://VYgo/images/powers/ygo.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new StringVar("YgoInfo")
    ];

    public void InitInfo() {
        var coreCard = this.YgoGetCore();
        StringVar stringVar = (StringVar)base.DynamicVars["YgoInfo"];
        int? level = Owner.Monster is BaseMonster monster ? monster.Level : coreCard?.Level;
        string formattedInfo = coreCard?.GetFormatedInfo(level) ?? string.Empty;
        if (formattedInfo.Length > 0) {
            stringVar.StringValue = formattedInfo;
        }
        else {
            stringVar.StringValue = "暂无信息";
        }
    }

    public override LocString Title {
        get {
            if (Card == null) Card = this.YgoGetCard();
            if (Card != null) return Card.TitleLocString;

            var monster = this.YgoGetMonster();
            if (monster != null) return monster.Title;
            return base.Title;
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips {
        get {
            var list = new List<IHoverTip>();
            var card = this.YgoGetCard();
            if (card != null) list.Add(HoverTipFactory.FromCard(card));
            return list;
        }
    }

    public override Creature ModifyUnblockedDamageTarget(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer
    ) {
        if (!IsGuardian) return target;
        if (Owner.Monster is MinionModel minion && minion.Position != MinionPosition.Front) return target;

        if (target != Owner.PetOwner?.Creature) {
            var shouldKeepTarget = true;

            if (target.PetOwner == Owner.PetOwner
                && Owner.PetOwner != null
                && target.GetPower<YgoPower>() is { IsGuardian: true }) {
                var pets = target.PetOwner.PlayerCombatState!.Pets;
                if (pets.IndexOf(Owner) < pets.IndexOf(target)) {
                    shouldKeepTarget = false;
                }
            }

            if (shouldKeepTarget) return target;
        }

        if (Owner.IsDead) return target;
        if (!props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered)) return target;

        return Owner;
    }

    public int CardId {
        get {
            if (Card != null) return Card.CardId;
            return Owner.Monster is BaseMonster monster
                ? monster.CardId
                : throw new InvalidOperationException("YgoPower owner is not a VYgo monster.");
        }
    }
}
