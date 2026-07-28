using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core;
using VYgo.Core.Hooks;
using VYgo.Core.Summon;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Common;

namespace VYgo.Scripts.Powers;

/// <summary>
/// Reduces an enemy buff or a debuff applied to the owner by an enemy,
/// consuming one stack for each power stack negated.
/// </summary>
[RegisterPower]
public class MaxxCPower : ModPowerTemplate, IMonsterSummonHookListener {
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/powers/maxx_c.png",
        BigIconPath: "res://VYgo/images/powers/maxx_c.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<MaxxC>(),
        YgoHoverTipConst.SpecialSummon(),
    ];

    public async Task AfterMonsterSummon(
        PlayerChoiceContext choiceContext,
        BaseMonsterCard card,
        CardPlay cardPlay,
        Creature summonedCreature,
        SummonContext summonContext) {
        if (summonContext.IsSpecialSummon && cardPlay.Player == Owner.Player) {
            for (int i = 0; (decimal)i < Amount; i++) {
                await CardPileCmd.Draw(choiceContext, cardPlay.Player);
            }
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants) {
        if (side == CombatSide.Player) {
            await PowerCmd.TickDownDuration(this);
        }
    }
}