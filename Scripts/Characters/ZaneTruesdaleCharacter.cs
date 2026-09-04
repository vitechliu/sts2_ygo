using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Scaffolding.Visuals;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Characters;

/// <summary>
/// 丸藤亮（凯撒亮）的正式可游玩角色实现。
/// 当前阶段复用 Redhat 的卡池，以及铁甲战士的遗物池和药水池。
/// </summary>
[RegisterCharacter]
public class ZaneTruesdaleCharacter
    : BaseYgoCharacter<ZaneTruesdaleCardPool, ZaneTruesdaleRelicPool, IroncladPotionPool>
{
    private const string AssetRoot = "res://VYgo/scenes/character/ZaneTruesdale";
    private const string ImageRoot = "res://VYgo/images/zane_truesdale";
    private const string AnimationRoot = "res://VYgo/images/zane_truesdale/animations";

    public override CardModel LargeCapsuleAttackCard => ModelDb.Card<CyberDragon>();
    public override CardModel LargeCapsuleDefenseCard =>  ModelDb.Card<CyberBarrierDragon>();
    
    public override Color NameColor => new("5dc8e8");
    public override Color EnergyLabelOutlineColor => new("10253f");
    public override Color MapDrawingColor => new("3aa7ca");

    public override CharacterGender Gender => CharacterGender.Masculine;
    public override int StartingHp => 80;
    public override int StartingGold => 99;
    
    protected override ModAnimStateMachine? SetupCustomCombatAnimationStateMachine(
        Node visualsRoot,
        CharacterModel character)
    {
        return ModAnimStateMachines.StandardCue(
            visualsRoot,
            character,
            idleName: "idle",
            hitName: "hit");
    }

    public override CharacterAssetProfile AssetProfile => CharacterAssetProfiles.Merge(
        CharacterAssetProfiles.Ironclad(),
        new(
            Scenes: new(
                VisualsPath: $"{AssetRoot}/zane_truesdale_character.tscn",
                EnergyCounterPath: $"{AssetRoot}/zane_truesdale_energy_counter.tscn",
                MerchantAnimPath: $"{AssetRoot}/zane_truesdale_merchant.tscn",
                RestSiteAnimPath: $"{AssetRoot}/zane_truesdale_rest_site.tscn"
            ),
            Ui: new(
                IconTexturePath: $"{ImageRoot}/icon.png",
                IconPath: $"{AssetRoot}/zane_truesdale_icon.tscn",
                CharacterSelectBgPath: $"{AssetRoot}/zane_truesdale_bg.tscn",
                CharacterSelectIconPath: $"{ImageRoot}/character_select.png",
                CharacterSelectLockedIconPath: $"{ImageRoot}/character_select_locked.png",
                MapMarkerPath: $"{ImageRoot}/icon.png"
            )
        ) {
            VisualCues = ModVisualCues.CueSet()
                .Sequence("idle", 
                    seq => BuildFrames(seq, AnimationRoot + "/idle/", 0.06f, 0, 27)
                )
                .Sequence("hit", 
                    seq => BuildFrames(seq, AnimationRoot + "/hit/", 0.04f, 0, 21)
                )
                .Build()
        }
    );

    public override float AttackAnimDelay => 0f;
    public override float CastAnimDelay => 0f;
    public override bool RequiresEpochAndTimeline => false;
}
