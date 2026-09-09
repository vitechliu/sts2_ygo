using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core;
using VYgo.Core.Hooks;
using VYgo.Core.Summon;
using VYgo.Scripts.Cards;
namespace VYgo.Scripts.Powers;
[RegisterPower]
public class CynetCodecPower : ModPowerTemplate, IMonsterSummonHookListener {
    private sealed class Data { public bool Upgraded; }
    protected override object InitInternalData() => new Data();
    public void Configure(bool upgraded) => GetInternalData<Data>().Upgraded = upgraded;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override PowerAssetProfile AssetProfile => new(IconPath: "res://VYgo/images/powers/ygo.png", BigIconPath: "res://VYgo/images/powers/ygo.png");
    public async Task AfterMonsterSummon(PlayerChoiceContext choiceContext, BaseMonsterCard card, CardPlay cardPlay,
        Creature summonedCreature, SummonContext summonContext) {
        if (!summonContext.IsSpecialSummon || card.Owner != Owner.Player || !card.ContainArchetype(YgoArchetypes.CodeTalker)
            || Owner.Player is not { } player) return;
        var attribute = card.YgoGetCore()?.Attribute;
        if (string.IsNullOrEmpty(attribute)) return;
        var choices = CardFactory.GetDistinctForCombat(player, ModelDb.AllCards.OfType<BaseMonsterCard>()
            .Where(candidate => !candidate.IsExtra && candidate.Rarity != CardRarity.Token
                && candidate.YgoGetCore().IsRace(YgoRace.Cyberse) && candidate.YgoGetCore()?.Attribute == attribute),
            1, player.RunState.Rng.CombatCardGeneration).ToList();
        if (GetInternalData<Data>().Upgraded) CardCmd.Upgrade(choices, CardPreviewStyle.HorizontalLayout);
        foreach (var generated in choices) await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, player);
    }
}
