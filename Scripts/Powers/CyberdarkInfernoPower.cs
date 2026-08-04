using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core;
using VYgo.Core.Hooks;
using VYgo.Core.Summon;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Fusion;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;
using VYgo.Utils;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public class CyberdarkInfernoPower : BaseActionPower, IMonsterSummonHookListener {
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/powers/cyberdark_inferno_power.png",
        BigIconPath: "res://VYgo/images/powers/cyberdark_inferno_power.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<CyberdarkInferno>(),
        HoverTipFactory.FromCard<Polymerization>(),
        YgoHoverTipConst.Enhance(),
        YgoHoverTipConst.Action(),
    ];

    public async Task AfterMonsterSummon(
        PlayerChoiceContext choiceContext,
        BaseMonsterCard card,
        CardPlay cardPlay,
        Creature summonedCreature,
        SummonContext summonContext) {
        if (cardPlay.Player != Owner.Player
            || !card.ContainArchetype(YgoArchetypes.Cyberdark)
            || !summonedCreature.IsAlive) {
            return;
        }

        Flash();
        await MinionUtil.AddHp(summonedCreature, Amount);
    }

    protected override async Task<bool> OnAction(ModRightClickExecutionContext context) {
        if (context.PlayerChoiceContext == null) return false;

        Flash();
        await PowerCmd.Remove(this);

        CardModel polymerization = context.Player.Creature.CombatState
            .CreateCard<Polymerization>(context.Player);
        await CardPileCmd.AddGeneratedCardToCombat(
            polymerization,
            PileType.Hand,
            context.Player);
        return true;
    }
}
