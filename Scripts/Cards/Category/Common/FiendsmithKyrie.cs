using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(YgoEventCardPool))]
public class FiendsmithKyrie()
    : BaseTrapCard(1, CardType.Power, CardRarity.Event, TargetType.None),
        IModRightClickableCard {
    public override int CardId => 26434972;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<FiendsmithKyriePower>(),
        HoverTipFactory.FromPower<FiendsmithKyrieDamageReductionPower>(),
        YgoHoverTipConst.SetCard(),
        YgoHoverTipConst.PowerAction(),
        YgoHoverTipConst.GraveyardAction(),
        YgoHoverTipConst.FusionSummon()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        await PowerCmd.Apply<FiendsmithKyriePower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
    }

    public bool CanExecuteRightClick(ModRightClickExecutionContext context) {
        return context.PlayerChoiceContext != null
            && context.Player == Owner
            && Pile?.Type == PileType.Discard
            && SummonUtil.HasFusionSummonTarget(
                Owner,
                _ => GetGraveyardFusionMaterials(),
                _ => PileType.Discard,
                IsFiendsmithFusionMonster);
    }

    public async Task OnRightClick(ModRightClickExecutionContext context) {
        if (!CanExecuteRightClick(context)
            || context.PlayerChoiceContext is not { } choiceContext) {
            return;
        }

        NCapstoneContainer.Instance?.Close();
        ExtraDeckSummonResult result = await SummonUtil.ExecuteFusionSummon(
            new FusionSummonRequest(
                SourceCard: this,
                Owner: Owner,
                ChoiceContext: choiceContext,
                SelectionPrompt: SelectionScreenPrompt,
                GetAvailableMaterials: _ => GetGraveyardFusionMaterials(),
                GetMaterialDestination: _ => PileType.Discard,
                FusionCardFilter: IsFiendsmithFusionMonster));
        if (result.Success && Pile?.Type == PileType.Discard) {
            await CardCmd.Exhaust(choiceContext, this);
        }
    }

    private IReadOnlyList<SummonMaterial> GetGraveyardFusionMaterials() {
        List<SummonMaterial> materials = SummonUtil.GetFieldMonsterMaterials(Owner).ToList();
        materials.AddRange(SummonUtil.GetEquippedMonsterMaterials(Owner));
        return materials;
    }

    private static bool IsFiendsmithFusionMonster(BaseExtraFusionCard card) {
        return card.ContainArchetype(YgoArchetypes.Fiendsmith);
    }
}
