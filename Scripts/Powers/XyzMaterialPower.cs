using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public class XyzMaterialPower : ModPowerTemplate {
    private List<CardModel> _materials = [];

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public IReadOnlyList<CardModel> Materials => _materials;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/powers/xyz_material_power.png",
        BigIconPath: "res://VYgo/images/powers/xyz_material_power.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        _materials.Select(card => HoverTipFactory.FromCard(card));

    internal bool InitializeMaterials(IReadOnlyList<CardModel> cards) {
        AssertMutable();
        if (_materials.Count > 0
            || cards.Count != Amount
            || cards.Count == 0
            || cards.Distinct().Count() != cards.Count) {
            return false;
        }

        _materials = [..cards];
        return true;
    }

    internal bool AttachMaterial(CardModel card) {
        AssertMutable();
        if (_materials.Contains(card)) return false;
        _materials.Add(card);
        return true;
    }

    internal bool DetachMaterial(CardModel card) {
        AssertMutable();
        return _materials.Remove(card);
    }

    internal List<CardModel> DetachAllMaterials() {
        AssertMutable();
        List<CardModel> cards = [.._materials];
        _materials.Clear();
        return cards;
    }

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength
    ) {
        if (creature != Owner || wasRemovalPrevented) return;
        await XyzMaterialCmd.SendAllToGraveyard(choiceContext, this);
    }

    public override async Task AfterRemoved(Creature oldOwner) {
        if (_materials.Count == 0) return;
        await XyzMaterialCmd.SendAllToGraveyard(
            new ThrowingPlayerChoiceContext(),
            this
        );
    }

    protected override void DeepCloneFields() {
        base.DeepCloneFields();
        _materials = [.._materials];
    }
}
