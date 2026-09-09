using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Interactions.RightClick;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core;
using VYgo.Core.Cards;
using VYgo.Utils;
namespace VYgo.Scripts.Cards.Category.Playmaker;
[RegisterCard(typeof(PlaymakerCardPool))]
public class ProtectcodeTalker() : BaseExtraLinkCard(-1, CardType.Skill, CardRarity.Rare, TargetType.None), IModRightClickableCard {
    public override int CardId => 58036229;
    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 10;
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Concat([new DynamicVar("Boost", 5)]);
    public override int GetLinkMaterialCount(CoreCard card) => 2;
    public override bool CanUseLinkMaterial(SummonMaterial material) => material.IsEffectMonster;
    private SummonMaterialSelectionSpec BuildGraveSelection() => new(
        PileType.Discard.GetPile(Owner).Cards.Where(card => card != this && card is BaseExtraLinkCard)
            .Select(SummonMaterial.FromMonsterCard).ToList(), 1, 3,
        materials => materials.Sum(material => material.CoreCard?.LinkCount ?? 0) == 3);
    public bool CanExecuteRightClick(ModRightClickExecutionContext context) =>
        context.PlayerChoiceContext != null && context.Player == Owner && Pile?.Type == PileType.Discard
        && Owner.MinionCount() < Owner.GetMaxMinionCount() && BuildGraveSelection().HasValidCombination;
    public async Task OnRightClick(ModRightClickExecutionContext context) {
        if (!CanExecuteRightClick(context) || context.PlayerChoiceContext is not { } choiceContext) return;
        var materials = await SummonMaterialSelectCmd.Select(choiceContext, Owner, this, BuildGraveSelection);
        if (materials.Count == 0 || materials.Sum(material => material.CoreCard?.LinkCount ?? 0) != 3
            || materials.Any(material => material.Card?.Pile?.Type != PileType.Discard)) return;
        foreach (var material in materials) await CardCmd.Exhaust(choiceContext, material.Card!);
        if (Pile?.Type == PileType.Discard && Owner.MinionCount() < Owner.GetMaxMinionCount())
            await CardCmd.AutoPlay(choiceContext, this, null);
    }
}
