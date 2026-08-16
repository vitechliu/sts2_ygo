using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Scaffolding.Visuals;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Characters;

/// <summary>
/// Playmaker 的可游玩角色外壳。
/// 专属卡牌与初始遗物会在后续内容设计阶段补充。
/// </summary>
[RegisterCharacter]
public class PlaymakerCharacter
    : BaseYgoCharacter<PlaymakerCardPool, PlaymakerRelicPool, IroncladPotionPool>
{
    private const string AssetRoot = "res://VYgo/scenes/character/Playmaker";
    private const string ImageRoot = "res://VYgo/images/playmaker";

    public override Color NameColor => new("48e7ff");
    public override Color EnergyLabelOutlineColor => new("24153f");
    public override Color MapDrawingColor => new("20bad1");

    public override CharacterGender Gender => CharacterGender.Masculine;
    public override int StartingHp => 80;
    public override int StartingGold => 99;

    public override CharacterAssetProfile AssetProfile => CharacterAssetProfiles.Merge(
        CharacterAssetProfiles.Ironclad(),
        new(
            Scenes: new(
                VisualsPath: $"{AssetRoot}/playmaker_character.tscn",
                EnergyCounterPath: $"{AssetRoot}/playmaker_energy_counter.tscn",
                MerchantAnimPath: $"{AssetRoot}/playmaker_merchant.tscn",
                RestSiteAnimPath: $"{AssetRoot}/playmaker_rest_site.tscn"
            ),
            Ui: new(
                IconTexturePath: $"{ImageRoot}/icon.png",
                IconPath: $"{AssetRoot}/playmaker_icon.tscn",
                CharacterSelectBgPath: $"{AssetRoot}/playmaker_bg.tscn",
                CharacterSelectIconPath: $"{ImageRoot}/character_select.png",
                CharacterSelectLockedIconPath: $"{ImageRoot}/character_select_locked.png",
                MapMarkerPath: $"{ImageRoot}/icon.png"
            )
        ) {
            VisualCues = ModVisualCues.CueSet()
                .Single("idle", $"{ImageRoot}/idle.png")
                .Build()
        }
    );

    public override float AttackAnimDelay => 0f;
    public override float CastAnimDelay => 0f;
    public override bool RequiresEpochAndTimeline => false;
    
#pragma warning disable CS0672 // Member overrides obsolete member
    protected override IEnumerable<Type> StartingRelicTypes => [
#pragma warning restore CS0672 // Member overrides obsolete member
        typeof(BagOfPreparation),
    ];
}
