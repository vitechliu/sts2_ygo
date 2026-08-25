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
using VYgo.Core.Hooks;
using VYgo.Core.Summon;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Playmaker;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public class CynetRegressionPower : ModPowerTemplate, IMonsterSummonHookListener {
    private sealed class Data {
        public CardModel? SourceCard { get; set; }
        public decimal Damage { get; set; }
        public int Draw { get; set; }
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/cards/19943114.png",
        BigIconPath: "res://VYgo/images/cards/19943114.png");

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<CynetRegression>(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        YgoHoverTipConst.SetCard(),
        YgoHoverTipConst.SpecialSummon()
    ];

    protected override object InitInternalData() {
        return new Data();
    }

    public void Configure(CardModel sourceCard, decimal damage, int draw) {
        AssertMutable();
        Data data = GetInternalData<Data>();
        data.SourceCard = sourceCard;
        data.Damage = damage;
        data.Draw = draw;
    }

    public async Task AfterMonsterSummon(
        PlayerChoiceContext choiceContext,
        BaseMonsterCard card,
        CardPlay cardPlay,
        Creature summonedCreature,
        SummonContext summonContext) {
        if (!summonContext.IsSpecialSummon
            || cardPlay.Player != Owner.Player
            || card is not BaseExtraLinkCard
            || Owner.Player is not { } player) {
            return;
        }

        Data data = GetInternalData<Data>();
        if (data.SourceCard == null) return;

        Flash();
        await CardCmd.Exhaust(choiceContext, data.SourceCard);
        await PowerCmd.Remove(this);
        await DamageCmd.Attack(data.Damage)
            .FromCard(data.SourceCard, null)
            .TargetingAllOpponents(Owner.CombatState)
            .Execute(choiceContext);
        await CardPileCmd.Draw(choiceContext, data.Draw, player);
    }
}
