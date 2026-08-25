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
using VYgo.Core;
using VYgo.Core.Hooks;
using VYgo.Core.Summon;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Utils;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public class CynetStormPower : ModPowerTemplate, IMonsterSummonHookListener {
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/cards/42461852.png",
        BigIconPath: "res://VYgo/images/cards/42461852.png");

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<CynetStorm>(),
        YgoHoverTipConst.Enhance(),
        YgoHoverTipConst.SpecialSummon()
    ];

    public async Task AfterMonsterSummon(
        PlayerChoiceContext choiceContext,
        BaseMonsterCard card,
        CardPlay cardPlay,
        Creature summonedCreature,
        SummonContext summonContext) {
        if (cardPlay.Player != Owner.Player
            || card is not BaseExtraLinkCard
            || !summonedCreature.IsAlive) {
            return;
        }

        Flash();
        await PowerCmd.Apply<AttackPower>(
            choiceContext,
            summonedCreature,
            Amount,
            Owner,
            card);
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource) {
        if (target != Owner
            || result.UnblockedDamage <= 20
            || Owner.Player is not { } player
            || player.MinionCount() >= player.GetMaxMinionCount()) {
            return;
        }

        BaseExtraLinkCard? selected = player.RunState.Rng.CombatCardSelection.NextItem(
            Entry.ExtraPile.GetPile(player).Cards
                .OfType<BaseExtraLinkCard>()
                .Where(card => card.YgoGetCore().IsRace(YgoRace.Cyberse))
                .ToList());
        if (selected == null) return;

        Flash();
        await selected.AutoPlayAndCaptureSummonedCreature(choiceContext, null);
        await PowerCmd.Remove(this);
    }
}
