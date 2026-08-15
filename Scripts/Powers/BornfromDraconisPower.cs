using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;
using VYgo.Utils;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public class BornfromDraconisPower : BaseActionPower {
    private sealed class Data {
        public CardModel? SourceCard { get; set; }
        public int SetTurnNumber { get; set; }
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/cards/96699830.png",
        BigIconPath: "res://VYgo/images/cards/96699830.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar("SummonCount", 1),
        new DynamicVar("BoostAttack", 5m),
        new DynamicVar("BoostLife", 5m),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<BornfromDraconis>(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        YgoHoverTipConst.SetCard(),
        YgoHoverTipConst.PowerAction(),
        YgoHoverTipConst.SpecialSummon(),
        YgoHoverTipConst.Enhance(),
    ];

    protected override object InitInternalData() {
        return new Data();
    }

    public void Configure(int summonCount, int boostAttack, int boostLife) {
        AssertMutable();
        DynamicVars["SummonCount"].BaseValue = summonCount;
        DynamicVars["BoostAttack"].BaseValue = boostAttack;
        DynamicVars["BoostLife"].BaseValue = boostLife;
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource) {
        Data data = GetInternalData<Data>();
        data.SourceCard = cardSource;
        data.SetTurnNumber = Owner.Player?.PlayerCombatState.TurnNumber ?? 0;
        return Task.CompletedTask;
    }

    public override bool CanExecuteRightClick(ModRightClickExecutionContext context) {
        Data data = GetInternalData<Data>();
        return base.CanExecuteRightClick(context)
            && data.SourceCard != null
            && context.Player.PlayerCombatState.TurnNumber > data.SetTurnNumber
            && context.Player.MinionCount() < MinionUtil.MaxMinionCount
            && PileType.Hand.GetPile(context.Player).Cards.Any(IsLightMachineMonster);
    }

    protected override async Task<bool> OnAction(ModRightClickExecutionContext context) {
        if (context.PlayerChoiceContext == null) return false;

        Data data = GetInternalData<Data>();
        if (data.SourceCard == null) return false;

        BaseMonsterCard? selectedMonster = (await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(
                    SelectionScreenPrompt,
                    DynamicVars["SummonCount"].IntValue),
                context: context.PlayerChoiceContext,
                pile: PileType.Hand.GetPile(context.Player),
                player: context.Player,
                filter: IsLightMachineMonster))
            .OfType<BaseMonsterCard>()
            .FirstOrDefault();
        if (selectedMonster == null) return false;

        Creature? summonedCreature = await selectedMonster.AutoPlayAndCaptureSummonedCreature(
            context.PlayerChoiceContext,
            null);
        if (summonedCreature == null) return false;

        List<BaseMonsterCard> monstersToExhaust = PileType.Discard.GetPile(context.Player).Cards
            .OfType<BaseMonsterCard>()
            .Where(IsLightMachineMonster)
            .ToList();
        int exhaustedCount = 0;
        foreach (BaseMonsterCard monsterCard in monstersToExhaust) {
            await CardCmd.Exhaust(context.PlayerChoiceContext, monsterCard);
            if (monsterCard.Pile?.Type == PileType.Exhaust) {
                exhaustedCount++;
            }
        }

        Flash();
        if (exhaustedCount > 0) {
            await PowerCmd.Apply<AttackPower>(
                context.PlayerChoiceContext,
                summonedCreature,
                exhaustedCount * DynamicVars["BoostAttack"].BaseValue,
                context.Player.Creature,
                data.SourceCard);
            await MinionUtil.AddHp(
                summonedCreature,
                exhaustedCount * DynamicVars["BoostLife"].IntValue);
        }

        await CardPileCmd.Add(data.SourceCard, PileType.Discard);
        await PowerCmd.Remove(this);
        return true;
    }

    private static bool IsLightMachineMonster(CardModel card) {
        if (card is not BaseMonsterCard monsterCard) return false;

        var coreCard = monsterCard.YgoGetCore();
        return coreCard.IsRace(YgoRace.Machine)
            && coreCard?.Attribute == "光";
    }
}
