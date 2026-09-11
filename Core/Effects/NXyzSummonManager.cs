using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using VYgo.Utils;

namespace VYgo.Core.Effects;

/// <summary>使用源 Prefab 层级、绑定曲线和粒子参数。各阶段共用时钟与深度缓冲。</summary>
public partial class NXyzSummonManager : Node3D {
    private const string Root = "res://VYgo/scenes/summon/xyz/";
    public const float Duration = 5.016667f;
    private const float GalaxyStart = 1.166667f, TrailStart = 1.7f, ExplosionStart = 2.95f, ResultStart = 3.683333f;
    private readonly List<Stage> _stages = [];
    private readonly List<MeshInstance3D> _cards = [];
    private readonly Dictionary<string, Mesh> _meshes = [];
    private Camera3D _camera = null!;
    private Node3D _stageRoot = null!;
    private JsonDocument _source = null!;
    private Shader _shader = null!;
    private Shader _holeShader = null!;
    private Shader _cardShader = null!;
    private MeshInstance3D _resultBand = null!;

    public override void _Ready() {
        _camera = GetNode<Camera3D>("Camera3D");
        _stageRoot = GetNode<Node3D>("StageRoot");
        _source = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(Root + "xyz_source.json"));
        _shader = GD.Load<Shader>(Root + "xyz_source_fx.gdshader");
        _holeShader = GD.Load<Shader>(Root + "xyz_hole.gdshader");
        _cardShader = GD.Load<Shader>(Root + "xyz_card.gdshader");
        var bandMaterial = new ShaderMaterial { Shader = _shader, RenderPriority = -1 };
        bandMaterial.SetShaderParameter("mode", 6);
        bandMaterial.SetShaderParameter("main_tex", GD.Load<Texture2D>(Root + "assets/fxt_fre_006.png"));
        _resultBand = new MeshInstance3D {
            Mesh = new QuadMesh { Size = Vector2.One }, MaterialOverride = bandMaterial, Visible = false,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        _stageRoot.AddChild(_resultBand);
        // MDPro3 CameraManager：位置 (0,95,-37)，俯角 70，竖直视角 30。
        // Unity 左手坐标转换为 Godot 时反转 Z，不能重复旋转导入网格。
        _camera.Position = new Vector3(0, 95, 37);
        _camera.RotationDegrees = new Vector3(-70, 0, 0);
        _camera.Fov = 30;
        _camera.KeepAspect = Camera3D.KeepAspectEnum.Height;
        _camera.Near = 0.05f;
        _camera.Far = 400;
        _camera.Current = true;
        var environment = new Godot.Environment {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0,0,0,0),
            GlowEnabled = true, GlowIntensity = 0.8f, GlowStrength = 0.8f,
            GlowBloom = 0, GlowHdrThreshold = 1.2f,
            GlowBlendMode = Godot.Environment.GlowBlendModeEnum.Additive
        };
        environment.SetGlowLevel(0,0.8f);
        environment.SetGlowLevel(1,0.35f);
        AddChild(new WorldEnvironment { Environment = environment });
    }

    public override void _ExitTree() => _source?.Dispose();

    public async Task Play(IReadOnlyList<Card3DEffectContext> cards, int materialCount, bool slowMaterials = false) {
        if (cards.Count != materialCount + 1) throw new InvalidOperationException("超量演出卡牌捕获不完整。");
        BuildStage("galaxy", GalaxyStart);
        BuildStage("hole", GalaxyStart);
        BuildStage($"trail{Math.Clamp(materialCount, 1, 3)}", TrailStart);
        BuildStage("explosion", ExplosionStart);
        BuildStage("post", ResultStart);
        foreach (Card3DEffectContext card in cards) {
            card.DisplaySprite.Visible = false;
            card.GlowSprite.Visible = false;
            var texture = (Texture2D)card.CardMaterial.GetShaderParameter("card_texture");
            var material = new ShaderMaterial { Shader = _cardShader };
            material.SetShaderParameter("card_texture", texture);
            var mesh = new MeshInstance3D {
                // 四边等量留白，柔光与卡面共用网格和透视变换。
                Mesh = new QuadMesh { Size = new Vector2(7.2f * texture.GetWidth() / texture.GetHeight(), 7.2f) * (1.12f / 1.05f) }, MaterialOverride = material,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
            };
            _stageRoot.AddChild(mesh);
            _cards.Add(mesh);
        }
        float elapsed = 0;
        bool materialSound = false, trailSound = false, explosionSound = false, resultSound = false;
        SFXUtil.Play("event:/vygo/sfx/xyz_01");
        while (IsInsideTree() && elapsed < Duration) {
            if (!materialSound && elapsed >= 1.333333f) {
                materialSound = true;
                SFXUtil.Play($"event:/vygo/sfx/xyz_02_0{Math.Clamp(materialCount, 1, 3)}");
            }
            if (!trailSound && elapsed >= 2.066667f) { trailSound = true; SFXUtil.Play("event:/vygo/sfx/xyz_material"); }
            if (!explosionSound && elapsed >= 3.05f) { explosionSound = true; SFXUtil.Play("event:/vygo/sfx/xyz_03"); }
            if (!resultSound && elapsed >= 3.783333f) { resultSound = true; SFXUtil.Play("event:/vygo/sfx/xyz_04"); }
            foreach (Stage stage in _stages) UpdateStage(stage, elapsed - stage.Start);
            UpdateCards(elapsed, materialCount);
            await this.AwaitProcessFrame();
            if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
            // 仅开发指令可慢放素材段；正式演出与游戏全局时间不受影响。
            elapsed += (float)GetProcessDeltaTime() * (slowMaterials && elapsed < 1.333333f ? 0.2f : 1f);
        }
    }

    private void UpdateCards(float time, int count) {
        Vector2 viewport = GetViewport().GetVisibleRect().Size;
        // 源 CircleFrare 的金色叠层在视频中形成外侧流动光带；只保留一段弧，避免多层同心环。
        float bandAge = time-ResultStart-0.15f;
        _resultBand.Visible = bandAge >= 0;
        if (_resultBand.Visible) {
            _resultBand.Position = _camera.ProjectPosition(viewport*new Vector2(0.49f,0.5f),31);
            _resultBand.Basis = _camera.Basis.ScaledLocal(new Vector3(21,17,1));
            var bandMaterial = (ShaderMaterial)_resultBand.MaterialOverride;
            bandMaterial.SetShaderParameter("arc_angle",2.8f+bandAge*1.7f);
            bandMaterial.SetShaderParameter("particle_color",new Color(1,1,1,Mathf.Clamp(bandAge/0.12f,0,1)*Mathf.Clamp((Duration-time)/0.2f,0,1)));
        }
        for (int i = 0; i < _cards.Count; i++) {
            bool result = i == count;
            var mesh = _cards[i];
            float local = result ? time - ResultStart : time;
            mesh.Visible = result ? local >= 0 : time < 1.333333f;
            if (!mesh.Visible) continue;
            float fade = result ? 1 - Mathf.Clamp((local - 1.15f) / 0.183334f, 0, 1) : Mathf.Clamp(time / 0.15f, 0, 1);
            // 源 ShowUnitCard 最后 5 帧沿父层局部 Z 上飞；不能误当相机深度。
            float retreat = result ? 0 : Mathf.Clamp((time - 1.25f) / 0.083333f, 0, 1);
            float fly = retreat * retreat;
            float hover = Mathf.SmoothStep(0, 1, Mathf.Clamp(time / 1.25f, 0, 1));
            float side = count == 1 ? 1 : (i - (count - 1) * 0.5f) / ((count - 1) * 0.5f);
            float depth = result ? Mathf.Lerp(35, 30, Mathf.Clamp(local / 0.166667f, 0, 1)) : 30 + hover * 0.5f + fly * 3f;
            float x = result ? 0 : (i - (count - 1) * 0.5f) * (count == 2 ? 0.24f : Math.Min(0.2f, 0.76f / count));
            mesh.Position = _camera.ProjectPosition(viewport * new Vector2(0.5f + x, 0.5f - fly * 1.2f), depth);
            // 与连接悬浮相同：实际改变局部俯仰/偏航，保留动态卡面捕获与统一场景深度。
            float yaw = result ? Mathf.Lerp(-14, 8, Mathf.Clamp(local / 0.833333f, 0, 1))
                : (count == 1 ? Mathf.Lerp(-16, 16, hover) : side * Mathf.Lerp(12, 25, hover));
            float pitch = result ? Mathf.Lerp(-8, 3, Mathf.Clamp(local / 0.833333f, 0, 1)) : Mathf.Lerp(-3, -10, hover);
            float roll = result ? Mathf.Lerp(-5.203f, 5f, Mathf.Clamp(local / 0.833333f, 0, 1)) : side * 1.5f * hover;
            if (!result && count == 2) {
                // ShowUnitCard02 的 InfiniteClip 另有 Z=-25/+25 偏移；卡面在源 XZ 平面，
                // 因此对应这里的反向 Y 偏航。左右键分别从 0/0.5 秒开始，不能同相位镜像。
                float turn = Mathf.SmoothStep(0, 1, Mathf.Clamp((time - (i == 0 ? 0 : 0.5f)) / (i == 0 ? 1.25f : 0.75f), 0, 1));
                yaw = i == 0 ? 25f - 9.9699f * turn : -25f + 9.969917f * turn;
                pitch = -1.26738f * turn;
                roll = (i == 0 ? 2.71936f : -2.71936f) * turn;
            }
            float exitYaw = !result && count == 2 ? -side * 90f : side * 65f;
            Vector3 angles = new(Mathf.Lerp(pitch, -35, fly), Mathf.Lerp(yaw, exitYaw, fly), roll);
            mesh.Basis = _camera.Basis * Basis.FromEuler(angles * (Mathf.Pi / 180f));
            mesh.Scale = Vector3.One * (result ? 1.3f : Math.Min(1.3f, 4.4f / count));
            // 卡面靠离屏消失，不在屏幕中央提前淡掉。
            ((ShaderMaterial)mesh.MaterialOverride).SetShaderParameter("opacity", fade);
        }
    }

    private void BuildStage(string name, float start) {
        JsonElement data = _source.RootElement.GetProperty("stages").GetProperty(name);
        var stageRoot = new Node3D { Name = name };
        _stageRoot.AddChild(stageRoot);
        var stage = new Stage(stageRoot, data, start);
        stage.Nodes[""] = stageRoot;
        foreach (JsonElement item in data.GetProperty("nodes").EnumerateArray().OrderBy(n => S(n, "path").Length == 0 ? -1 : S(n, "path").Count(c => c == '/'))) {
            string path = S(item, "path");
            Node3D node;
            if (path.Length == 0) node = stageRoot;
            else {
                node = new Node3D { Name = path.Split('/')[^1] };
                string parent = path.Contains('/') ? path[..path.LastIndexOf('/')] : "";
                stage.Nodes[parent].AddChild(node);
                stage.Nodes[path] = node;
            }
            node.Position = UnityVector(item.GetProperty("position"));
            JsonElement rotation = item.GetProperty("rotation");
            node.Quaternion = new Quaternion(-F(rotation, "x"), -F(rotation, "y"), F(rotation, "z"), F(rotation, "w"));
            node.Scale = Vector(item.GetProperty("scale"));
            if (!item.TryGetProperty("material", out var matId) || matId.ValueKind != JsonValueKind.String) continue;
            JsonElement mat = _source.RootElement.GetProperty("materials").GetProperty(matId.GetString()!);
            string matName = S(mat, "name");
            // 实际 STS2 卡面代替 Unity 占位卡；无纹理辅助对象不进入画面。
            if (matName is "lambert1" or "ParticlesUnlit" or "postXYZcardAdd" || matName.StartsWith("DummyCard")) continue;
            ShaderMaterial material = MakeMaterial(mat);
            stage.Materials[path] = material;
            if (item.TryGetProperty("particle", out JsonElement particle)) {
                int alignment = (int)F(item,"renderAlignment");
                Vector3 pivot = item.TryGetProperty("pivot",out var pivotData) ? UnityVector(pivotData) : Vector3.Zero;
                if (F(item, "renderMode") == 4 || path == "CircleFrare") {
                    Mesh mesh = path == "CircleFrare" ? new QuadMesh { Size = Vector2.One } : LoadMesh(S(item, "mesh"));
                    var visual = new MeshInstance3D { Mesh = mesh, MaterialOverride = material,
                        CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
                    node.AddChild(visual);
                    if (path == "CircleFrare") visual.TopLevel = true;
                    stage.Particles.Add(new Particle(node, visual, null, particle, path, material, alignment, pivot));
                } else {
                    var emitter = new MultiMeshInstance3D { Multimesh = new MultiMesh {
                        TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseColors = true,
                        Mesh = new QuadMesh { Size = Vector2.One }, InstanceCount = Math.Clamp((int)F(particle.GetProperty("InitialModule"), "maxNumParticles"), 1, 2000),
                        VisibleInstanceCount = 0 }, MaterialOverride = material,
                        CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
                    node.AddChild(emitter);
                    emitter.TopLevel = true;
                    emitter.GlobalTransform = Transform3D.Identity;
                    stage.Particles.Add(new Particle(node, null, emitter, particle, path, material, alignment, pivot));
                }
            } else if (item.TryGetProperty("mesh", out JsonElement meshName)) {
                node.AddChild(new MeshInstance3D { Mesh = LoadMesh(meshName.GetString()!),
                    MaterialOverride = material, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
            }
        }
        stage.Root.Visible = false;
        _stages.Add(stage);
    }

    private ShaderMaterial MakeMaterial(JsonElement source) {
        var mat = new ShaderMaterial { Shader = _shader };
        string name = S(source, "name");
        if (name == "hole") mat.Shader = _holeShader;
        if (name == "hole") mat.RenderPriority = -20;
        int mode = name switch { "Ita" => 1, "XYZInMesh01" => 2, "Column" => 3, "HemiSphere" => 4, "explosionBtm" => 5, "CardParticle" => 5, _ => 0 };
        mat.SetShaderParameter("mode", mode);
        JsonElement textures = source.GetProperty("textures");
        string texture = textures.TryGetProperty("_MainTex", out var main) ? main.GetString() ?? "" : "";
        if (mode == 2) texture = textures.GetProperty("_Texture2D").GetString()!;
        if (texture.Length == 0) mode = 5;
        mat.SetShaderParameter("mode", mode);
        if (texture.Length > 0) mat.SetShaderParameter("main_tex", GD.Load<Texture2D>(Root + "assets/" + texture));
        if (textures.TryGetProperty("_Mask", out var mask)) mat.SetShaderParameter("mask_tex", GD.Load<Texture2D>(Root + "assets/" + mask.GetString()));
        var colors = source.GetProperty("colors");
        mat.SetShaderParameter("tint", colors.TryGetProperty("_TintColor", out var tint) ? Color(tint) : colors.TryGetProperty("_Color", out tint) ? Color(tint) : Colors.White);
        mat.SetShaderParameter("gain", name switch { "Ita" => 1.2f, "CircleFrare" => 6f, "CenterFrare" => 10f, "EFF_flrcmn_01a_MAT" => 4f, _ => 2f });
        if (name == "CenterFrare") {
            // 只让中心耀斑的白核覆盖背景，外围蓝晕仍用加法混合。
            mat.SetShaderParameter("opaque_core", true);
            mat.SetShaderParameter("alpha_gain", 2f);
        }
        if (name == "EFF_flrcmn_01a_MAT") mat.SetShaderParameter("alpha_gain",2f);
        JsonElement floats = source.GetProperty("floats");
        if (mode == 5) {
            mat.SetShaderParameter("spot_power", Math.Max(0.5f, F(floats, "_Alphapow")));
            mat.SetShaderParameter("spot_blur", Math.Max(0.15f, F(floats, "_BaseBlur")));
            mat.SetShaderParameter("gain", Math.Max(0.5f, F(floats, "_Alpha")));
            mat.SetShaderParameter("spot_white_core", name == "explosionBtm");
        }
        return mat;
    }

    private Mesh LoadMesh(string name) {
        if (_meshes.TryGetValue(name,out var existing)) return existing;
        if (!_source.RootElement.GetProperty("coloredMeshes").TryGetProperty(name,out var data)) return _meshes[name]=GD.Load<Mesh>(Root+"assets/"+name);
        var arrays = new Godot.Collections.Array(); arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = data.GetProperty("vertices").EnumerateArray().Select(v=>new Vector3(v[0].GetSingle(),v[1].GetSingle(),v[2].GetSingle())).ToArray();
        arrays[(int)Mesh.ArrayType.Normal] = data.GetProperty("normals").EnumerateArray().Select(v=>new Vector3(v[0].GetSingle(),v[1].GetSingle(),v[2].GetSingle())).ToArray();
        arrays[(int)Mesh.ArrayType.TexUV] = data.GetProperty("uv").EnumerateArray().Select(v=>new Vector2(v[0].GetSingle(),v[1].GetSingle())).ToArray();
        arrays[(int)Mesh.ArrayType.Color] = data.GetProperty("colors").EnumerateArray().Select(v=>new Color(v[0].GetSingle(),v[1].GetSingle(),v[2].GetSingle(),v[3].GetSingle())).ToArray();
        arrays[(int)Mesh.ArrayType.Index] = data.GetProperty("indices").EnumerateArray().Select(v=>v.GetInt32()).ToArray();
        var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);
        return _meshes[name]=mesh;
    }

    private void UpdateBillboards(Particle particle, float age) {
        MultiMesh instances = particle.Emitter!.Multimesh;
        if (age < 0) { instances.VisibleInstanceCount = 0; return; }
        JsonElement data = particle.Data, initial = data.GetProperty("InitialModule"), emission = data.GetProperty("EmissionModule");
        Transform3D current = particle.Node.GlobalTransform;
        bool first = particle.PreviousAge < 0;
        float previous = first ? 0 : particle.PreviousAge;
        float delta = age - previous;
        float length = Math.Max(0.001f, F(data, "lengthInSec"));
        // Unity InitialModuleUI 将 moveWithTransform 作为 simulationSpace 枚举：0 本地、1 世界。
        bool localSpace = F(data, "moveWithTransform") == 0;
        particle.Live.RemoveAll(p => age - p.Birth >= p.Lifetime);
        int births = 0;
        bool looping = particle.Looping ?? F(data, "looping") == 1;
        if (age <= length || looping) {
            particle.EmissionRemainder += Scalar(emission.GetProperty("rateOverTime"), (looping ? age % length : age) / length) * delta;
            births = (int)particle.EmissionRemainder;
            particle.EmissionRemainder -= births;
        }
        foreach (JsonElement burst in emission.GetProperty("m_Bursts").EnumerateArray()) {
            float at = F(burst, "time");
            if ((first && at == 0) || (previous < at && age >= at)) births += (int)Scalar(burst.GetProperty("countCurve"));
        }
        births = Math.Min(births, instances.InstanceCount - particle.Live.Count);
        for (int i = 0; i < births; i++) {
            int serial = ++particle.Serial;
            float random = RandomUnit(serial, 1), angle = RandomUnit(serial, 2) * Mathf.Tau;
            float birth = first ? 0 : Mathf.Lerp(previous, age, (i + 1f) / Math.Max(1, births));
            float normalized = (looping ? birth % length : birth) / length;
            Vector3 position = Vector3.Zero, direction = Vector3.Up;
            JsonElement shape = data.GetProperty("ShapeModule");
            if (F(shape, "enabled") == 1) {
                float radius = F(shape.GetProperty("radius"), "value");
                int type = (int)F(shape, "type");
                if (type == 17) {
                    // Unity ParticleSystemShapeType.Donut=17；小环截面在大环周围分散星点。
                    float minorAngle = RandomUnit(serial,3)*Mathf.Tau;
                    float thickness = Mathf.Clamp(F(shape,"radiusThickness"),0,1);
                    float minorRadius = F(shape,"donutRadius")*Mathf.Sqrt(Mathf.Lerp((1-thickness)*(1-thickness),1,RandomUnit(serial,4)));
                    Vector3 radial = new(Mathf.Cos(angle),Mathf.Sin(angle),0);
                    direction = radial*Mathf.Cos(minorAngle)+Vector3.Forward*Mathf.Sin(minorAngle);
                    position = radial*radius+direction*minorRadius;
                } else {
                    direction = type == 10 ? new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0)
                        : new Vector3(Mathf.Cos(angle), RandomUnit(serial, 4)*2-1, Mathf.Sin(angle)).Normalized();
                    float thickness = Mathf.Clamp(F(shape,"radiusThickness"),0,1);
                    float inner = 1-thickness;
                    float radial = type == 10 ? Mathf.Sqrt(Mathf.Lerp(inner*inner,1,RandomUnit(serial,3))) : Mathf.Pow(Mathf.Lerp(inner*inner*inner,1,RandomUnit(serial,3)),1f/3f);
                    position = direction * radius * radial;
                }
                if (shape.TryGetProperty("m_Scale", out var shapeScale)) position *= Vector(shapeScale);
                if (shape.TryGetProperty("m_Rotation",out var shapeRotation)) {
                    Vector3 angles = Vector(shapeRotation)*Mathf.Pi/180;
                    Basis basis = Basis.FromEuler(new Vector3(-angles.X,-angles.Y,angles.Z),EulerOrder.Yxz);
                    position = basis*position; direction = basis*direction;
                }
                if (shape.TryGetProperty("m_Position", out var shapePosition)) position += UnityVector(shapePosition);
            }
            Transform3D transform = first ? current : particle.PreviousTransform.InterpolateWith(current, (i+1f)/Math.Max(1,births));
            Vector3 velocity = direction * Sample(initial.GetProperty("startSpeed"), normalized, random);
            float size = Sample(initial.GetProperty("startSize"), normalized, random);
            float sizeY = F(initial, "size3D") == 1 ? Sample(initial.GetProperty("startSizeY"), normalized, random) : size;
            // 源叠加相机的近景粒子换算到共用透视舞台，分别校准黑洞和结果光环。
            if (particle.Path.Contains("hole")) { size *= 0.3f; sizeY *= 0.3f; }
            if (particle.Path == "Column/ColumnBtm") { size *= 0.65f; sizeY *= 0.65f; }
            if (!localSpace) { position = transform * position; velocity = transform.Basis * velocity; }
            Vector3 scaling = transform.Basis.Scale.Abs();
            float rotation = initial.TryGetProperty("startRotation", out var initialRotation) ? Sample(initialRotation, normalized, random) : 0;
            JsonElement spinModule = data.GetProperty("RotationModule");
            float spin = F(spinModule, "enabled") == 1 ? Scalar(spinModule.GetProperty("curve")) : 0;
            Color color = SampleColor(initial.GetProperty("startColor"), normalized, random);
            particle.Live.Add(new Billboard(birth, Math.Max(0.01f, Sample(initial.GetProperty("startLifetime"),normalized,random)),
                position, velocity, new Vector2(size*scaling.X, sizeY*scaling.Y), rotation, spin, color, random));
        }
        JsonElement sizes = data.GetProperty("SizeModule"), colors = data.GetProperty("ColorModule");
        int index = 0;
        foreach (Billboard p in particle.Live) {
            float lifetime = age - p.Birth, normalized = Mathf.Clamp(lifetime/p.Lifetime,0,1);
            Vector2 size = p.Size;
            if (F(sizes,"enabled") == 1) {
                float x = Sample(sizes.GetProperty("curve"), normalized, p.Random);
                size *= new Vector2(x, F(sizes,"separateAxes") == 1 ? Sample(sizes.GetProperty("y"), normalized,p.Random) : x);
            }
            Color color = p.Color;
            if (F(colors,"enabled") == 1) color *= GradientColor(colors.GetProperty("gradient").GetProperty("maxGradient"),normalized);
            if (particle.Path.EndsWith("holeLoop")) {
                size *= Mathf.Lerp(0.1f,1,Mathf.SmoothStep(0,1,Mathf.Clamp(age/0.7f,0,1)));
                color.A *= Mathf.Clamp(age/0.3f,0,1) * Mathf.Clamp((1.3f-age)/0.35f,0,1);
            }
            Vector3 position = p.Position + p.Velocity*lifetime;
            if (localSpace) position = current * position;
            if (particle.Path.Contains("hole")) position = _camera.ProjectPosition(GetViewport().GetVisibleRect().Size*0.5f,100);
            Basis facing = particle.Alignment == 2 ? current.Basis.Orthonormalized() : _camera.GlobalBasis;
            Basis basis = facing * new Basis(Vector3.Back, p.Rotation + p.Spin*lifetime);
            basis = basis.ScaledLocal(new Vector3(Math.Max(size.X,0.0001f),Math.Max(size.Y,0.0001f),1));
            position += basis * particle.Pivot;
            instances.SetInstanceTransform(index, new Transform3D(basis,position));
            instances.SetInstanceColor(index++, color);
        }
        instances.VisibleInstanceCount = index;
        particle.PreviousAge = age;
        particle.PreviousTransform = current;
    }

    private static float RandomUnit(int serial, int channel) {
        uint n = unchecked((uint)(serial*747796405 + channel*289133645));
        n = ((n >> ((int)(n >> 28) + 4)) ^ n) * 277803737u;
        return ((n >> 22) ^ n) / (float)uint.MaxValue;
    }
    private static float Sample(JsonElement value, float time, float random) {
        int mode = (int)F(value,"minMaxState");
        float maximum = Scalar(value,time);
        if (mode == 3) return Mathf.Lerp(F(value,"minScalar"),maximum,random);
        if (mode == 2) return Mathf.Lerp(F(value,"minScalar")*ObjectCurve(value.GetProperty("minCurve").GetProperty("m_Curve"),time),maximum,random);
        return maximum;
    }
    private static Color SampleColor(JsonElement value, float time, float random) => (int)F(value,"minMaxState") switch {
        1 => GradientColor(value.GetProperty("maxGradient"),time),
        2 => Color(value.GetProperty("minColor")).Lerp(Color(value.GetProperty("maxColor")),random),
        3 => GradientColor(value.GetProperty("minGradient"),time).Lerp(GradientColor(value.GetProperty("maxGradient"),time),random),
        4 => GradientColor(value.GetProperty("maxGradient"),random),
        _ => Color(value.GetProperty("maxColor"))
    };
    private static Color GradientColor(JsonElement gradient, float time) {
        int count = (int)F(gradient,"m_NumColorKeys");
        Color previous = Color(gradient.GetProperty("key0"));
        float previousTime = F(gradient,"ctime0")/65535f;
        for (int i=1;i<count;i++) {
            float nextTime = F(gradient,"ctime"+i)/65535f;
            Color next = Color(gradient.GetProperty("key"+i));
            if (time <= nextTime) {
                if (F(gradient,"m_Mode") != 1) previous = previous.Lerp(next,Mathf.Clamp((time-previousTime)/Math.Max(0.0001f,nextTime-previousTime),0,1));
                break;
            }
            previous = next; previousTime = nextTime;
        }
        previous.A = GradientAlpha(gradient,time);
        return previous;
    }

    private void UpdateStage(Stage stage, float time) {
        float stageDuration = stage.Root.Name == "galaxy" ? Duration-GalaxyStart : stage.Start == GalaxyStart ? 2.416667f : 2f;
        stage.Root.Visible = time >= 0 && time <= stageDuration;
        if (!stage.Root.Visible) return;
        Dictionary<string, float> activeTimes = [];
        // Main 中保留的循环黑洞没有激活轨，随 Galaxy 开始并在爆发前收束。
        if (stage.Root.Name == "hole" && time >= 0.65f) activeTimes["Offset/holeLoop"] = time - 0.65f;
        foreach (JsonElement track in stage.Data.GetProperty("tracks").EnumerateArray()) {
            string target = S(track, "target");
            float local = time - F(track, "start");
            if (S(track, "kind") == "active") {
                float duration = F(track,"duration");
                // 参考录屏在结果卡展示末段仍保留膨胀光环，持续到本次卡面淡出。
                if (target == "CircleFrare") duration = Duration-stage.Start-F(track,"start");
                bool active = local >= 0 && local < duration;
                stage.Nodes[target].Visible = active;
                if (active) activeTimes[target] = local;
                continue;
            }
            float clipTime = Mathf.Clamp(local, 0, F(track, "duration")) * F(track, "speed") + F(track, "clipIn");
            Dictionary<Node3D, Quaternion> rotations = [];
            foreach (JsonElement curve in track.GetProperty("curves").EnumerateArray()) {
                string child = S(curve, "path");
                string path = target + (target.Length > 0 && child.Length > 0 ? "/" : "") + child;
                if (!stage.Nodes.TryGetValue(path, out Node3D? node)) continue;
                string attribute = S(curve, "attribute");
                float value = Curve(curve.GetProperty("keys"), clipTime);
                if (attribute.StartsWith("material.")) {
                    if (stage.Materials.TryGetValue(path, out var material)) SetMaterialCurve(material, attribute, value);
                } else if (attribute.StartsWith("m_LocalPosition.")) {
                    int axis = Axis(attribute); Vector3 position = node.Position; position[axis] = axis == 2 ? -value : value; node.Position = position;
                } else if (attribute.StartsWith("localEulerAnglesRaw.")) {
                    int axis = Axis(attribute); Vector3 rotation = node.RotationDegrees; rotation[axis] = axis == 2 ? value : -value; node.RotationDegrees = rotation;
                } else if (attribute.StartsWith("m_LocalScale.")) {
                    Vector3 scale = node.Scale; scale[Axis(attribute)] = value; node.Scale = scale;
                } else if (attribute.StartsWith("m_LocalRotation.")) {
                    Quaternion q = rotations.GetValueOrDefault(node, node.Quaternion);
                    switch (attribute[^1]) { case 'x': q.X = -value; break; case 'y': q.Y = -value; break; case 'z': q.Z = value; break; case 'w': q.W = value; break; }
                    rotations[node] = q;
                } else if (attribute == "m_IsActive") node.Visible = value > 0.5f;
                else if (attribute == "looping") {
                    Particle? particle = stage.Particles.FirstOrDefault(p => p.Path == path);
                    if (particle != null) particle.Looping = value > 0.5f;
                }
            }
            foreach (var rotation in rotations) rotation.Key.Quaternion = rotation.Value.Normalized();
        }
        foreach (Particle particle in stage.Particles) {
            string owner = activeTimes.Keys.Where(k => particle.Path == k || particle.Path.StartsWith(k + "/")).OrderByDescending(k => k.Length).FirstOrDefault() ?? "";
            bool active = owner.Length > 0 && particle.Node.IsVisibleInTree();
            float age = active ? activeTimes[owner] : -1;
            JsonElement initial = particle.Data.GetProperty("InitialModule");
            float lifetime = Scalar(initial.GetProperty("startLifetime"));
            if (particle.Path == "CircleFrare" && particle.Mesh != null) {
                particle.Mesh.Visible = active;
                if (active) {
                    float diameter = Mathf.Lerp(0,13.5f,Mathf.SmoothStep(0,1,Mathf.Clamp(age/0.4f,0,1)));
                    float remaining = Duration-stage.Start-time;
                    float fade = Mathf.Clamp(age/0.1f,0,1)*Mathf.Clamp(remaining/0.2f,0,1);
                    particle.Mesh.GlobalPosition = _camera.ProjectPosition(GetViewport().GetVisibleRect().Size*0.5f,30.5f);
                    particle.Mesh.Basis = _camera.GlobalBasis.ScaledLocal(new Vector3(Math.Max(diameter,0.001f),Math.Max(diameter,0.001f),1));
                    particle.Mesh.RotateObjectLocal(Vector3.Back,age*0.4f);
                    // 出卡瞬间有短促白亮峰值，随后回到单层绿色球壳，不增发第二个环。
                    float peak = 1+2.5f*Mathf.Clamp((0.28f-age)/0.16f,0,1);
                    particle.Material.SetShaderParameter("particle_color",new Color(0.95f*peak,1.25f*peak,0.7f*peak,fade));
                }
                continue;
            }
            if (particle.Emitter != null) {
                UpdateBillboards(particle, age);
                continue;
            }
            if (particle.Mesh == null) continue;
            if (Scalar(particle.Data.GetProperty("EmissionModule").GetProperty("rateOverTime")) > 0 && age > lifetime) age %= lifetime;
            particle.Mesh.Visible = active && age <= lifetime;
            if (!particle.Mesh.Visible) continue;
            float p = Mathf.Clamp(age / lifetime, 0, 1);
            var sizeModule = particle.Data.GetProperty("SizeModule");
            float size = Scalar(initial.GetProperty("startSize"));
            bool separate = F(initial, "size3D") == 1;
            Vector3 scale = new(size, separate ? Scalar(initial.GetProperty("startSizeY")) : size, separate ? Scalar(initial.GetProperty("startSizeZ")) : size);
            if (F(sizeModule, "enabled") == 1) {
                bool axes = F(sizeModule, "separateAxes") == 1;
                float x = Scalar(sizeModule.GetProperty("curve"), p);
                scale *= new Vector3(x, axes ? Scalar(sizeModule.GetProperty("y"), p) : x, axes ? Scalar(sizeModule.GetProperty("z"), p) : x);
            }
            // 仅压窄光柱截面，保留原上冲高度及所有其他组的投影。
            if (particle.Path == "Column") scale *= new Vector3(0.65f,1,0.65f);
            // 第一段幕布按录屏占屏比例收小，第二段实光带不随之缩放。
            if (particle.Path.StartsWith("Aurora")) scale *= 0.85f;
            particle.Mesh.Scale = scale.Max(Vector3.One * 0.0001f);
            particle.Mesh.Position = scale * particle.Pivot;
            var color = Color(initial.GetProperty("startColor").GetProperty("maxColor"));
            var colorModule = particle.Data.GetProperty("ColorModule");
            if (F(colorModule, "enabled") == 1) color *= GradientColor(colorModule.GetProperty("gradient").GetProperty("maxGradient"), p);
            if (particle.Path.StartsWith("Aurora")) {
                // 幕布覆盖中心耀斑及回落，在光柱起始时退走，不能围住整个出卡阶段。
                color.A = Mathf.Clamp(age/0.12f,0,1)*Mathf.Clamp((ExplosionStart+0.05f-GalaxyStart-time)/0.2f,0,1);
            }
            particle.Material.SetShaderParameter("particle_color", color);
        }
    }

    private static void SetMaterialCurve(ShaderMaterial material, string attribute, float value) {
        string? parameter = attribute switch {
            "material._TilingAndOffset.x" => "trail_tiling", "material._TilingAndOffset.z" => "trail_offset",
            "material._PositionOffset" => "position_offset", "material._MaskTilingOffset.x" => "mask_scale_x",
            "material._MaskTilingOffset.y" => "mask_scale_y", "material._MaskTilingOffset.z" => "mask_offset_x",
            "material._MaskTilingOffset.w" => "mask_offset_y", _ => null
        };
        if (parameter != null) material.SetShaderParameter(parameter, value);
    }
    private static float Scalar(JsonElement value, float time = 0) {
        float scalar = F(value, "scalar");
        if ((int)F(value, "minMaxState") is 1 or 2) return scalar * ObjectCurve(value.GetProperty("maxCurve").GetProperty("m_Curve"), time);
        return scalar;
    }
    private static float ObjectCurve(JsonElement keys, float time) => Evaluate(keys.EnumerateArray().Select(k => new Key(F(k,"time"), F(k,"value"), Slope(k,"inSlope"), Slope(k,"outSlope"))).ToArray(), time);
    private static float Curve(JsonElement keys, float time) => Evaluate(keys.EnumerateArray().Select(k => new Key(k[0].GetSingle(), k[1].GetSingle(), k[2].ValueKind == JsonValueKind.Number ? k[2].GetSingle() : float.PositiveInfinity, k[3].ValueKind == JsonValueKind.Number ? k[3].GetSingle() : float.PositiveInfinity)).ToArray(), time);
    private static float Evaluate(Key[] keys, float time) {
        if (keys.Length == 0) return 1;
        if (time <= keys[0].Time) return keys[0].Value;
        for (int i = 1; i < keys.Length; i++) {
            Key a = keys[i - 1], b = keys[i];
            if (time > b.Time) continue;
            if (!float.IsFinite(a.Out) || !float.IsFinite(b.In)) return a.Value;
            float dt = b.Time - a.Time, t = (time - a.Time) / dt, t2 = t * t, t3 = t2 * t;
            return (2*t3-3*t2+1)*a.Value + (t3-2*t2+t)*dt*a.Out + (-2*t3+3*t2)*b.Value + (t3-t2)*dt*b.In;
        }
        return keys[^1].Value;
    }
    private static float GradientAlpha(JsonElement gradient, float time) {
        int count = (int)F(gradient, "m_NumAlphaKeys");
        float previousTime = 0, previous = F(gradient.GetProperty("key0"), "a");
        for (int i = 1; i < count; i++) {
            float nextTime = F(gradient, "atime" + i) / 65535f, next = F(gradient.GetProperty("key" + i), "a");
            if (time <= nextTime) return Mathf.Lerp(previous, next, Mathf.Clamp((time - previousTime) / Math.Max(0.0001f, nextTime - previousTime), 0, 1));
            previousTime = nextTime; previous = next;
        }
        return previous;
    }
    private static int Axis(string attribute) => attribute[^1] == 'x' ? 0 : attribute[^1] == 'y' ? 1 : 2;
    private static float Slope(JsonElement e, string key) => e.GetProperty(key).ValueKind == JsonValueKind.Number ? F(e, key) : float.PositiveInfinity;
    private static float F(JsonElement e, string key) => e.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetSingle() : 0;
    private static string S(JsonElement e, string key) => e.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";
    private static Vector3 Vector(JsonElement e) => new(F(e,"x"), F(e,"y"), F(e,"z"));
    private static Vector3 UnityVector(JsonElement e) => new(F(e,"x"), F(e,"y"), -F(e,"z"));
    private static Color Color(JsonElement e) => new(F(e,"r"), F(e,"g"), F(e,"b"), F(e,"a"));
    private sealed record Key(float Time, float Value, float In, float Out);
    private sealed record Stage(Node3D Root, JsonElement Data, float Start) {
        public Dictionary<string, Node3D> Nodes { get; } = [];
        public Dictionary<string, ShaderMaterial> Materials { get; } = [];
        public List<Particle> Particles { get; } = [];
    }
    private sealed record Particle(Node3D Node, MeshInstance3D? Mesh, MultiMeshInstance3D? Emitter, JsonElement Data, string Path, ShaderMaterial Material, int Alignment, Vector3 Pivot) {
        public bool? Looping { get; set; }
        public float PreviousAge { get; set; } = -1;
        public float EmissionRemainder { get; set; }
        public int Serial { get; set; }
        public Transform3D PreviousTransform { get; set; }
        public List<Billboard> Live { get; } = [];
    }
    private sealed record Billboard(float Birth, float Lifetime, Vector3 Position, Vector3 Velocity, Vector2 Size, float Rotation, float Spin, Color Color, float Random);
}
