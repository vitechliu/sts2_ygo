using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public sealed class CyberValleyGuardForm : BaseCyberValleyOption {
    protected override string PortraitFileName => "cyber_valley_guard_form.png";

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(CyberValley.GuardBlock, ValueProp.Move),
        new CardsVar(CyberValley.GuardDraw),
    ];

    public override async Task OnChosen(PlayerChoiceContext choiceContext) {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }
}
