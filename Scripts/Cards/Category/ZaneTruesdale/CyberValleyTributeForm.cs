using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public sealed class CyberValleyTributeForm : BaseCyberValleyOption {
    protected override string PortraitFileName => "cyber_valley_tribute_form.png";

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(CyberValley.TributeDraw),
    ];

    public override async Task OnChosen(PlayerChoiceContext choiceContext) {
        bool tributeSucceeded = await SummonUtil.ExecuteFieldTribute(
            choiceContext,
            Owner,
            this,
            1
        );
        if (tributeSucceeded) {
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        }
    }
}
