using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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
    private List<CardModel> _equippedCards = [];

    // 类型，Buff或Debuff
    public override PowerType Type => PowerType.Buff;

    // 叠加类型，Counter表示可叠加，Single表示不可叠加
    public override PowerStackType StackType => PowerStackType.Single;

    public BaseVYgoCard? Card { get; set; }
    public IReadOnlyList<CardModel> EquippedCards => _equippedCards;

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
            list.AddRange(_equippedCards.Select(equipment => HoverTipFactory.FromCard(equipment)));
            return list;
        }
    }

    internal bool AttachEquipment(CardModel card) {
        AssertMutable();
        if (_equippedCards.Contains(card)) return false;

        _equippedCards.Add(card);
        return true;
    }

    internal bool DetachEquipment(CardModel card) {
        AssertMutable();
        return _equippedCards.Remove(card);
    }

    internal List<CardModel> DetachAllEquipment() {
        AssertMutable();
        List<CardModel> cards = _equippedCards.ToList();
        _equippedCards.Clear();
        return cards;
    }

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength) {
        if (creature != Owner || wasRemovalPrevented) return;
        await EquipCmd.SendAllToGraveyard(choiceContext, this);
    }

    public override async Task AfterRemoved(Creature oldOwner) {
        if (_equippedCards.Count == 0) return;
        await EquipCmd.SendAllToGraveyard(
            new ThrowingPlayerChoiceContext(),
            this);
    }

    protected override void DeepCloneFields() {
        base.DeepCloneFields();
        _equippedCards = [.._equippedCards];
    }

    public int CardId {
        get {
            if (Card != null) return Card.CardId;
            return (Owner.Monster as BaseMonster).CardId;
        }
    }
}
