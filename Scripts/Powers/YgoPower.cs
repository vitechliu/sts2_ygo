using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
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
        if (coreCard != null && coreCard.FormatedInfo.Length > 0) {
            stringVar.StringValue = coreCard.FormatedInfo;
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

    public int CardId {
        get {
            if (Card != null) return Card.CardId;
            return (Owner.Monster as BaseMonster).CardId;
        }
    }
}