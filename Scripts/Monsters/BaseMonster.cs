using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Powers;
using MinionLib.Minion;

namespace VYgo.Scripts.Monsters;

public abstract class BaseMonster: MinionModel
{
    public override int MinInitialHp => 6; // 作为敌方方怪物生成时的血量，通常无需在意
    public override int MaxInitialHp => 6; // 作为敌方方怪物生成时的血量，通常无需在意
    protected override string VisualsPath => "res://Example/MinionTest/scenes/creature_visuals/pettest_attackaka.tscn"; // 随从的视觉资源路径，tscn 格式，建议参考原版游戏的怪物
    
    // 召唤时执行的代码，通常用来设置血量、应用初始能力等，options 是在召唤随从时传入的参数
    public override async Task OnSummon(Player owner, Creature self, MinionSummonOptions options) // 注意使用 self 而非 this
    {
        // if (options.MaxHp is decimal maxHp)
        //     await CreatureCmd.SetMaxAndCurrentHp(self, maxHp); // 设置血量
        //
        // if (options.PrimaryStatAmount is decimal strength && strength > 0m)
        //     await PowerCmd.Apply<StrengthPower>(self, strength, owner.Creature, options.Source); // 根据传入的参数设置力量

    }
}