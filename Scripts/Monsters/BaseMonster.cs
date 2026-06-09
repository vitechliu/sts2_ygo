using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MinionLib.Minion;
using MinionLib.Powers;

namespace VYgo.Scripts.Monsters;

public abstract class BaseMonster: MinionModel
{
    public override int MinInitialHp => 6; // 作为敌方方怪物生成时的血量，通常无需在意
    public override int MaxInitialHp => 6; // 作为敌方方怪物生成时的血量，通常无需在意
    protected override string VisualsPath => "res://Example/MinionTest/scenes/creature_visuals/pettest_attackaka.tscn"; // 随从的视觉资源路径，tscn 格式，建议参考原版游戏的怪物
    
    public virtual bool IsGuardian {
        get;
        set;
    } = true;

    // 召唤时执行的代码，通常用来设置血量、应用初始能力等，options 是在召唤随从时传入的参数
    public override async Task OnSummon(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options) // 注意使用 self 而非 this
    {
        if (options.MaxHp is { } maxHp)
            await CreatureCmd.SetMaxAndCurrentHp(Creature, maxHp); // 设置血量
        if (IsGuardian)
            await PowerCmd.Apply<MinionGuardianPower>(choiceContext, Creature, 1m, owner.Creature, options.Source);
        if (options.PrimaryStatAmount is { } strength && strength > 0m)
            await PowerCmd.Apply<StrengthPower>(choiceContext, Creature, strength, owner.Creature, options.Source);
    }
}
