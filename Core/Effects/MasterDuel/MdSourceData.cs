using Godot;

namespace VYgo.Core.Effects.MasterDuel;

/// <summary>用 Resource 显式携带源数据与材质引用，确保导出包包含依赖。</summary>
[ScriptPath("res://Core/Effects/MasterDuel/MdSourceData.cs")]
public partial class MdSourceData : Resource {
    [Export] public string DataJson { get; set; } = "";
    [Export] public Godot.Collections.Array<ShaderMaterial> Materials { get; set; } = [];
}
