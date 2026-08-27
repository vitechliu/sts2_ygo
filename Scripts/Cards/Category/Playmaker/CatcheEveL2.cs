using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interactions.RightClick;
using VYgo.Core;
using VYgo.Scripts.Monsters;
using VYgo.Scripts.Powers;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class CatcheEveL2() : BaseRightClickableMonsterCard(1, CardType.Attack, CardRarity.Common, TargetType.None) {
    public override int CardId => 50690129;

    protected override RightClickType ClickType => RightClickType.Hand;
    
    public override int BaseAttackVar => 3;
    public override int BaseLifeVar => 2;
    public override int UpgradeAttackVar => 2;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.HandAction(),
        YgoHoverTipConst.SpecialSummon()
    ];

    public override bool CanExecuteRightClick(ModRightClickExecutionContext context) {
        return base.CanExecuteRightClick(context)
            && Pile?.Type == PileType.Hand
            && Owner.MinionCount() < Owner.GetMaxMinionCount()
            && GetTargets().Count > 0;
    }

    protected override async Task OnYgoRightClick(
        ModRightClickExecutionContext context
    ) {
        if (context.PlayerChoiceContext is not { } choiceContext) return;

        Dictionary<CardModel, Creature> targets = GetTargets();
        CardModel? selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                Entry.MonsterPile.GetPile(Owner),
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1),
                targets.ContainsKey))
            .FirstOrDefault();
        if (selected == null || !targets.TryGetValue(selected, out Creature? target)) return;

        int level = ((BaseMonster)target.Monster!).Level ?? 0;
        await MonsterLevelPower.SetLevel(
            choiceContext,
            target,
            level - 2,
            Owner.Creature,
            this);
        await CardCmd.AutoPlay(choiceContext, this, null);
    }

    private Dictionary<CardModel, Creature> GetTargets() {
        return Owner.Creature.Pets
            .Where(pet => pet.Monster is BaseMonster {
                SourceCard: BaseMonsterCard,
                Level: >= 3
            })
            .ToUniqueSourceCardTargets(nameof(CatcheEveL2));
    }
}
