using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace VYgo.Core.Effects;

/// <summary>连接素材的源层级和曲线；所有卡面与轮廓共用透视相机。</summary>
internal static class LinkMaterialPreview {
    internal const string SourcePath = "res://VYgo/scenes/summon/link/link_material_source.json";
    internal const float Duration = 1.3333334f;
    private const string ShaderPath = "res://VYgo/scenes/summon/link/link_material.gdshader";

    internal static async Task Play(IReadOnlyList<Card3DEffectContext> cards, Vector2 center,
        Action begin, Action openGate, bool slowExit) {
        if (cards.Count == 0) throw new InvalidOperationException("连接素材卡面捕获为空。");
        using var source = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(SourcePath));
        var data = source.RootElement.GetProperty(Math.Min(8, cards.Count).ToString());
        var room = NCombatRoom.Instance ?? throw new InvalidOperationException("连接演出需要战斗房间。");
        Vector2 size = room.GetViewportRect().Size;
        var display = new SubViewportContainer {
            Position = center - size * 0.5f, Size = size, Stretch = true,
            MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = 950,
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.PremultAlpha }
        };
        var viewport = new SubViewport {
            Size = new Vector2I((int)size.X, (int)size.Y), OwnWorld3D = true, TransparentBg = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always, HandleInputLocally = false
        };
        display.AddChild(viewport);
        var root = new Node3D(); viewport.AddChild(root);
        viewport.AddChild(new Camera3D {
            Position = new Vector3(0, 95, 37), RotationDegrees = new Vector3(-70, 0, 0),
            Fov = 30, Near = 0.05f, Far = 400, Current = true, KeepAspect = Camera3D.KeepAspectEnum.Height
        });
        room.CombatVfxContainer.AddChild(display);
        try {
            Dictionary<string, Node3D> nodes = new() { [""] = root };
            foreach (var item in data.GetProperty("nodes").EnumerateArray().OrderBy(n => S(n, "path").Length)) {
                string path = S(item, "path"); var node = root;
                if (path.Length > 0) {
                    node = new Node3D();
                    nodes[path.Contains('/') ? path[..path.LastIndexOf('/')] : ""].AddChild(node);
                    nodes[path] = node;
                }
                node.Position = UnityVector(item.GetProperty("position"));
                var q = item.GetProperty("rotation");
                node.Quaternion = new Quaternion(-F(q,"x"),-F(q,"y"),F(q,"z"),F(q,"w"));
                node.Scale = Vector(item.GetProperty("scale"));
            }
            var shader = GD.Load<Shader>(ShaderPath);
            List<ShaderMaterial> materials = [];
            int index = 0;
            foreach (var face in data.GetProperty("faces").EnumerateArray()) {
                var card = cards[index++];
                card.DisplaySprite.Visible = false; card.GlowSprite.Visible = false;
                var texture = (Texture2D)card.CardMaterial.GetShaderParameter("card_texture");
                var material = new ShaderMaterial { Shader = shader };
                material.SetShaderParameter("card_texture",texture); materials.Add(material);
                float height = 8.6f * texture.GetHeight() / card.DisplaySize.Y * 1.12f;
                nodes[face.GetString()!].AddChild(new MeshInstance3D {
                    Mesh = new QuadMesh { Size = new Vector2(height * texture.GetWidth()/texture.GetHeight(),height) },
                    Basis = new Basis(Vector3.Left,Vector3.Back,Vector3.Up),
                    Position = new Vector3(0,0.025f,0), MaterialOverride = material,
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
                });
            }
            List<Track> tracks = [];
            foreach (var track in data.GetProperty("tracks").EnumerateArray()) {
                string target = S(track,"target");
                var rotation = UnityEuler(Vector(track.GetProperty("offsetEuler")));
                var clip = new Transform3D(UnityEuler(Vector(track.GetProperty("clipEuler"))),UnityVector(track.GetProperty("clipPosition")));
                var offset = new Transform3D(rotation,UnityVector(track.GetProperty("offsetPosition"))) * clip;
                var curves = track.GetProperty("curves").EnumerateArray().Select(c => new Curve(
                    nodes[target + (S(c,"path").Length > 0 ? "/"+S(c,"path") : "")], S(c,"attribute"),
                    c.GetProperty("keys").EnumerateArray().Select(k => new Key(k[0].GetSingle(),k[1].GetSingle(),Slope(k[2]),Slope(k[3]))).ToArray())).ToArray();
                tracks.Add(new Track(nodes[target],offset,curves));
            }
            bool gate = false; float time = 0;
            begin();
            while (time < Duration) {
                foreach (var track in tracks) Apply(track,time);
                foreach (var material in materials) material.SetShaderParameter("opacity",Mathf.Clamp(time/0.166667f,0,1));
                if (!gate && time >= 1.183333f) { gate = true; openGate(); }
                await root.AwaitProcessFrame();
                if (!GodotObject.IsInstanceValid(root) || !root.IsInsideTree()) throw new TaskCanceledException("连接素材舞台已退出。");
                float next = time + (float)root.GetProcessDeltaTime() * (slowExit && time >= 1.25f ? 0.1f : 1f);
                time = slowExit && time < 1.25f ? Math.Min(next,1.25f) : next;
            }
            if (!gate) openGate();
        }
        finally { if (GodotObject.IsInstanceValid(display)) display.QueueFree(); }
    }

    private static void Apply(Track track,float time) {
        Dictionary<Node3D,Vector3> eulers = [],positions = [];
        foreach (var curve in track.Curves) {
            float v = Evaluate(curve.Keys,time); string attr = curve.Attribute;
            int axis = attr[^1] == 'x' ? 0 : attr[^1] == 'y' ? 1 : 2;
            if (attr.StartsWith("localEulerAnglesRaw.")) { var p = eulers.GetValueOrDefault(curve.Node); p[axis]=v; eulers[curve.Node]=p; }
            else if (attr.StartsWith("m_LocalPosition.")) { var p = positions.GetValueOrDefault(curve.Node); p[axis]=axis==2 ? -v : v; positions[curve.Node]=p; }
            else if (attr.StartsWith("m_LocalScale.")) { var p=curve.Node.Scale;p[axis]=v;curve.Node.Scale=p; }
        }
        foreach (var (node,euler) in eulers) node.Basis = (node==track.Target ? track.Offset.Basis : Basis.Identity)*UnityEuler(euler);
        foreach (var (node,position) in positions) node.Position = node==track.Target ? track.Offset*position : position;
    }
    private static float Evaluate(Key[] keys,float time) {
        if (keys.Length==0) return 0;
        if (time<=keys[0].Time) return keys[0].Value;
        for(int i=1;i<keys.Length;i++) {
            var a=keys[i-1];var b=keys[i];if(time>b.Time)continue;
            if(!float.IsFinite(a.Out)||!float.IsFinite(b.In))return a.Value;
            float dt=b.Time-a.Time,t=(time-a.Time)/dt,t2=t*t,t3=t2*t;
            return (2*t3-3*t2+1)*a.Value+(t3-2*t2+t)*dt*a.Out+(-2*t3+3*t2)*b.Value+(t3-t2)*dt*b.In;
        }
        return keys[^1].Value;
    }
    private static float Slope(JsonElement v)=>v.ValueKind==JsonValueKind.Number?v.GetSingle():float.PositiveInfinity;
    private static float F(JsonElement e,string key)=>e.GetProperty(key).GetSingle();
    private static string S(JsonElement e,string key)=>e.GetProperty(key).GetString()??"";
    private static Vector3 Vector(JsonElement e)=>new(F(e,"x"),F(e,"y"),F(e,"z"));
    private static Vector3 UnityVector(JsonElement e)=>new(F(e,"x"),F(e,"y"),-F(e,"z"));
    private static Basis UnityEuler(Vector3 e)=>Basis.FromEuler(new Vector3(-e.X,-e.Y,e.Z)*Mathf.Pi/180,EulerOrder.Yxz);
    private sealed record Track(Node3D Target,Transform3D Offset,Curve[] Curves);
    private sealed record Curve(Node3D Node,string Attribute,Key[] Keys);
    private readonly record struct Key(float Time,float Value,float In,float Out);
}
