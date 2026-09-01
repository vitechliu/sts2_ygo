using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;

namespace VYgo.Core;

/// <summary>
/// “除外”效果的统一结算入口。后续专属动画应集中接入此处。
/// </summary>
public static class BanishCmd {
    private const string HitVfxPath = "vfx/vfx_attack_slash";

    /// <summary>
    /// 去除目标至多 <paramref name="amount"/> 点最大生命和等量当前生命。
    /// 此效果不进入伤害管线，因此不受格挡、伤害倍率、荆棘、无实体或缓冲影响，
    /// 但当前生命归零时仍会执行正常死亡流程。
    /// </summary>
    public static async Task Banish(Creature target, decimal amount) {
        ArgumentNullException.ThrowIfNull(target);
        if (amount < 0m) {
            throw new ArgumentOutOfRangeException(nameof(amount), "除外数值不能为负数。");
        }
        if (amount == 0m || !target.IsAlive) return;

        decimal oldCurrentHp = target.CurrentHp;
        decimal newMaxHp = Math.Max(0m, target.MaxHp - amount);
        decimal newCurrentHp = Math.Max(0m, oldCurrentHp - amount);

        // 暂时复用原版攻击特效；未来的除外专属动画统一替换这里即可。
        VfxCmd.PlayOnCreature(target, HitVfxPath);

        target.SetMaxHpInternal(newMaxHp);
        target.SetCurrentHpInternal(newCurrentHp);

        decimal currentHpDelta = newCurrentHp - oldCurrentHp;
        if (currentHpDelta != 0m) {
            await Hook.AfterCurrentHpChanged(
                target.Player?.RunState
                    ?? target.CombatState?.RunState
                    ?? NullRunState.Instance,
                target.CombatState,
                target,
                currentHpDelta);
        }

        if (target.IsDead) {
            await CreatureCmd.Kill(target);
        }
    }
}
