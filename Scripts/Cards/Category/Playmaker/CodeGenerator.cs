using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using VYgo.Core;
namespace VYgo.Scripts.Cards.Category.Playmaker;
[RegisterCard(typeof(PlaymakerCardPool))]
public class CodeGenerator() : BaseMonsterCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.None), ILinkMaterialCard {
    public override int CardId => 30114823;
    public override int BaseAttackVar => 5;
    public override int BaseLifeVar => 3;
    public override int UpgradeAttackVar => 2;
    public bool CanUseFromHand(BaseExtraLinkCard target) => target.ContainArchetype(YgoArchetypes.CodeTalker);
    public async Task AfterUsedAsLinkMaterial(PlayerChoiceContext choiceContext, Player owner, BaseExtraLinkCard target) {
        if (!CanUseFromHand(target) || Pile?.Type != PileType.Discard || Owner != owner) return;
        var selected = (await CardSelectCmd.FromCombatPile(choiceContext, PileType.Draw.GetPile(owner), owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), card => card is BaseMonsterCard)).FirstOrDefault();
        if (selected?.Pile?.Type == PileType.Draw) await CardCmd.Discard(choiceContext, selected);
    }
}
