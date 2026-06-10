using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace VYgo.Scripts.Var;

public class AttackVar: DynamicVar {
    public AttackVar(string name, decimal baseValue) : base(name, baseValue) { }
    public AttackVar(int attack) : base("Attack", attack) { }
}