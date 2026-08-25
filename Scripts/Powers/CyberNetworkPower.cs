using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;
using VYgo.Utils;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public class CyberNetworkPower : ModPowerTemplate {
    private sealed class Data {
        public int RemainingTurns { get; set; } = 3;
        public int ExhaustCount { get; set; } = 1;
        public bool CountdownComplete { get; set; }
        public bool UpgradeSummons { get; set; }
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override int DisplayAmount => GetInternalData<Data>().RemainingTurns;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/cards/12670770.png",
        BigIconPath: "res://VYgo/images/cards/12670770.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<CyberNetwork>(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        YgoHoverTipConst.SpecialSummon(),
    ];

    protected override object InitInternalData() {
        return new Data();
    }

    public void Configure(int turns, int exhaustCount, bool upgradeSummons) {
        AssertMutable();
        Data data = GetInternalData<Data>();
        data.RemainingTurns = turns;
        data.ExhaustCount = exhaustCount;
        data.CountdownComplete = turns <= 0;
        data.UpgradeSummons = upgradeSummons;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants) {
        Data data = GetInternalData<Data>();
        if (data.CountdownComplete
            || side != Owner.Side
            || !participants.Contains(Owner)
            || Owner.Player is not { } player) {
            return;
        }

        List<BaseMonsterCard> candidates = PileType.Draw.GetPile(player).Cards
            .OfType<BaseMonsterCard>()
            .Where(IsMachineMonster)
            .ToList();
        for (int i = 0; i < data.ExhaustCount && candidates.Count > 0; i++) {
            BaseMonsterCard? randomMachineMonster = player.RunState.Rng.CombatCardSelection
                .NextItem(candidates);
            if (randomMachineMonster == null) break;

            candidates.Remove(randomMachineMonster);
            Flash();
            await CardCmd.Exhaust(choiceContext, randomMachineMonster);
        }

        data.RemainingTurns = Math.Max(0, data.RemainingTurns - 1);
        data.CountdownComplete = data.RemainingTurns == 0;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player) {
        Data data = GetInternalData<Data>();
        if (player != Owner.Player || !data.CountdownComplete) return;

        List<BaseMonsterCard> machineMonsters = PileType.Exhaust.GetPile(player).Cards
            .OfType<BaseMonsterCard>()
            .Where(IsMachineMonster)
            .ToList();

        Flash();
        await PowerCmd.Remove(this);

        foreach (BaseMonsterCard monsterCard in machineMonsters) {
            if (player.MinionCount() >= player.GetMaxMinionCount()) break;

            if (data.UpgradeSummons) {
                CardCmd.Upgrade(monsterCard, CardPreviewStyle.None);
            }
            await CardCmd.AutoPlay(choiceContext, monsterCard, null);
        }
    }

    private static bool IsMachineMonster(BaseMonsterCard card) {
        return card.YgoGetCore().IsRace(YgoRace.Machine);
    }
}
