using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Scaffolding.Visuals.Definition;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;
using VYgo.Core;
using VYgo.Core.Extensions;
using VYgo.Scripts.Cards.Placeholders;

namespace VYgo.Scripts.Characters;

[RegisterCharacter]
public abstract class BaseYgoCharacter<TCardPool, TRelicPool, TPotionPool>
    : ModCharacterTemplate<TCardPool, TRelicPool, TPotionPool>, ILargeCapsuleCardProvider
    where TCardPool : CardPoolModel
    where TRelicPool : RelicPoolModel
    where TPotionPool : PotionPoolModel
{

    protected override ModAnimStateMachine? SetupCustomCombatAnimationStateMachine(
        Node visualsRoot,
        CharacterModel character
    ) => ModAnimStateMachines.StandardCue(visualsRoot, character, idleName: "idle");

    public override float AttackAnimDelay => 0f;
    public override float CastAnimDelay => 0f;
    public override bool RequiresEpochAndTimeline => false;

    /// <summary>
    /// 巨大扭蛋为 YGO 角色加入的第一张牌。
    /// 具体角色可以覆写此属性，改为自己的起始攻击牌。
    /// </summary>
    public virtual CardModel LargeCapsuleAttackCard => ModelDb.Card<AttackBasic>();

    /// <summary>
    /// 巨大扭蛋为 YGO 角色加入的第二张牌。
    /// 具体角色可以覆写此属性，改为自己的起始防御牌。
    /// </summary>
    public virtual CardModel LargeCapsuleDefenseCard => ModelDb.Card<DefenseBasic>();

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
        => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.Scenes!.VisualsPath!);

    public override List<string> GetArchitectAttackVfx() => [
        "vfx/vfx_attack_blunt",
        "vfx/vfx_heavy_blunt",
        "vfx/vfx_attack_slash",
        "vfx/vfx_bloody_impact",
        "vfx/vfx_rock_shatter",
    ];


    protected VisualFrameSequenceBuilder BuildFrames(
        VisualFrameSequenceBuilder builder, string path, float duration, int from, int to) {
        for (var i = from; i <= to; i++) {
            builder.Frame(path + "key_#####.png".FormatWithNumber(i), duration);
        }
        return builder;
    }
}

/// <summary>
/// 为巨大扭蛋提供不依赖 Strike/Defend 标签的替代牌。
/// </summary>
public interface ILargeCapsuleCardProvider {
    CardModel LargeCapsuleAttackCard { get; }
    CardModel LargeCapsuleDefenseCard { get; }
}


public static class YgoCharacterExtensions {
    public static bool IsYgoCharacter(this Player player) {
        return player.Character.GetType().IsGenericTypeOf(typeof(BaseYgoCharacter<,,>));
    }
}
