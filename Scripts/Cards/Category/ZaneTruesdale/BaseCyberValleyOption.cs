using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

public abstract class BaseCyberValleyOption()
    : ModCardTemplate(-1, CardType.None, CardRarity.Token, TargetType.None, false) {
    protected abstract string PortraitFileName { get; }

    public override bool CanBeGeneratedInCombat => false;
    public override int MaxUpgradeLevel => 0;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://VYgo/images/cards/{PortraitFileName}",
        FramePath: "res://VYgo/images/frame/Skill/card_design0010.png"
    );

    public abstract Task OnChosen(PlayerChoiceContext choiceContext);
}
