using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace VYgo.Scripts.Var;

public class LifeVar: DynamicVar {
    public LifeVar(string name, decimal baseValue) : base(name, baseValue) { }
    public LifeVar(int life) : base("Life", life) { }
}