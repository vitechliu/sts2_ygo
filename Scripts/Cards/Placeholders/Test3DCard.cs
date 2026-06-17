using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core.Effects;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Placeholders;

[RegisterCard(typeof(RedhatCardPool))]
[RegisterCharacterStarterCard(typeof(RedhatCharacter), 1)]
public class Test3DCard() : BasePlaceholder(CardType.Skill, CardRarity.Common) {
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://VYgo/images/cards/neko.jpg"
    );
    
    private static readonly PackedScene _flipperScene = GD.Load<PackedScene>("res://VYgo/scenes/vfx/Card3DFlipper.tscn");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        await base.OnPlay(choiceContext, cardPlay);
        await Play3DCenterFlipAnimation();
    }

    async Task Play3DCenterFlipAnimation() {
        NCard? node = NCard.FindOnTable(this);
        if (node == null || !GodotObject.IsInstanceValid(node) || !node.IsInsideTree()) {
            return;
        }

        node.PlayPileTween?.FastForwardToCompletion();

        if (_flipperScene == null) {
            return;
        }

        var flipper = _flipperScene.Instantiate<Card3DFlipper>();
        if (flipper == null) {
            return;
        }

        Vector2 screenCenter = node.GetViewportRect().Size * 0.5f;
        await flipper.PlayFlip(node, screenCenter, duration: 1.4f, scaleMultiplier: 1.5f);
    }
}
