using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using VYgo.Utils;

namespace VYgo.Core.Effects;

/// <summary>仪式独立舞台：源层级、Timeline 偏移和 Hermite 曲线共用真实透视相机。</summary>
public partial class NRitualSummonManager : Node3D
{
    private const string Root = "res://VYgo/scenes/summon/ritual/";
    public const float Duration = 5.883333f;
    private readonly List<Stage> _stages = [];
    private readonly Dictionary<string, Mesh> _meshes = [];
    private readonly HashSet<string> _disabled = [];
    private readonly List<(MeshInstance3D Mesh, ShaderMaterial Material, bool Result)> _cards = [];
    private Camera3D _camera = null!;
    private Node3D _stageRoot = null!;
    private JsonDocument _source = null!;
    private Shader _shader = null!, _blendShader = null!, _cardShader = null!, _cardAddShader = null!;
    private readonly HashSet<int> _soundsPlayed = [];
    public float Elapsed { get; private set; }
    public float BackgroundOpacity { get; private set; }

    public override void _Ready()
    {
        _source = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(Root + "ritual_source.json"));
        _shader = GD.Load<Shader>(Root + "ritual_fx.gdshader");
        _blendShader = GD.Load<Shader>(Root + "ritual_blend.gdshader");
        _cardShader = GD.Load<Shader>(Root + "ritual_card.gdshader");
        _cardAddShader = GD.Load<Shader>(Root + "ritual_card_add.gdshader");
        _stageRoot = new Node3D(); AddChild(_stageRoot);
        _camera = new Camera3D
        {
            Position = new Vector3(0, 95, 37),
            RotationDegrees = new Vector3(-70, 0, 0),
            Fov = 30,
            KeepAspect = Camera3D.KeepAspectEnum.Height,
            Near = 0.05f,
            Far = 400,
            Current = true
        };
        AddChild(_camera);
        // 舞台内 HDR 柔光只扩散高亮边缘，普通卡面维持原纹理亮度。
        var environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0, 0, 0, 0),
            GlowEnabled = true,
            GlowIntensity = 0.65f,
            GlowStrength = 0.9f,
            GlowBloom = 0,
            GlowHdrThreshold = 1.0f,
            GlowBlendMode = Godot.Environment.GlowBlendModeEnum.Additive
        };
        environment.SetGlowLevel(0, 0.8f); environment.SetGlowLevel(1, 0.45f);
        AddChild(new WorldEnvironment { Environment = environment });
    }
    public override void _ExitTree() => _source?.Dispose();

    public async Task Play(IReadOnlyList<Card3DEffectContext> cards, int count, bool slowExit = false)
    {
        if (cards.Count != count + 1) throw new InvalidOperationException("仪式卡面捕获不完整。");
        BuildStage("main", cards, count);
        BuildStage("show" + Math.Clamp(count, 1, 6), cards, count);
        for (int i = 1; i <= 3; i++)
        {
            bool enabled = count switch { 1 => i == 1, 2 => i == 2, 3 => i == 3, 4 => i != 2, 5 => i != 1, _ => true };
            if (!enabled) _disabled.Add($"RitualTrailInSet/RitualTrailIn{i:00}");
        }
        while (IsInsideTree() && Elapsed < Duration)
        {
            PlaySoundAt(0, 0, "ritual_card");
            PlaySoundAt(1, 1.366667f, "ritual_01");
            PlaySoundAt(2, 2.383333f, "ritual_02_0" + Math.Clamp(count, 1, 3));
            PlaySoundAt(3, 3.583333f, "ritual_03");
            PlaySoundAt(4, 4.766667f, "ritual_04");
            foreach (Stage stage in _stages) UpdateStage(stage, Elapsed);
            foreach (var card in _cards)
            {
                float alpha = card.Result ? Mathf.Clamp((Duration - Elapsed) / 0.166667f, 0, 1) : Mathf.Clamp(Elapsed / 0.166667f, 0, 1);
                card.Material.SetShaderParameter("opacity", alpha);
                card.Material.SetShaderParameter("glow_strength", card.Result ? 0.25f : 0.7f);
            }
            await this.AwaitProcessFrame();
            if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) throw new TaskCanceledException("仪式舞台已退出。");
            // 诊断只慢放素材退出的五帧，全局时间和持久设置保持原值。
            Elapsed += (float)GetProcessDeltaTime() * (slowExit && Elapsed >= 1.25f && Elapsed < 1.333334f ? 0.1f : 1f);
        }
    }

    private void PlaySoundAt(int id, float at, string name)
    {
        if (Elapsed >= at && _soundsPlayed.Add(id)) SFXUtil.Play("event:/vygo/sfx/" + name);
    }

    private void BuildStage(string name, IReadOnlyList<Card3DEffectContext> cards, int count)
    {
        var data = _source.RootElement.GetProperty("stages").GetProperty(name);
        var root = new Node3D { Name = name }; _stageRoot.AddChild(root);
        var stage = new Stage(root, data, 0); stage.Nodes[""] = root;
        foreach (var item in data.GetProperty("nodes").EnumerateArray().OrderBy(n => S(n, "path").Length == 0 ? -1 : S(n, "path").Count(c => c == '/')))
        {
            string path = S(item, "path"); Node3D node = root;
            if (path.Length > 0)
            {
                node = new Node3D { Name = path.Split('/')[^1] };
                stage.Nodes[path.Contains('/') ? path[..path.LastIndexOf('/')] : ""].AddChild(node); stage.Nodes[path] = node;
            }
            node.Position = UnityVector(item.GetProperty("position"));
            var q = item.GetProperty("rotation"); node.Quaternion = new Quaternion(-F(q, "x"), -F(q, "y"), F(q, "z"), F(q, "w"));
            node.Scale = Vector(item.GetProperty("scale")); node.Visible = item.GetProperty("active").GetBoolean();
            if (!item.TryGetProperty("material", out var mid) || mid.ValueKind != JsonValueKind.String) continue;
            var mat = _source.RootElement.GetProperty("materials").GetProperty(mid.GetString()!); string mn = S(mat, "name");
            if (mn is "DummyCardHandFrontForTL" or "SummonRitualCardAdd")
            {
                bool overlay = mn == "SummonRitualCardAdd";
                bool result = name == "main";
                int index = result ? count : int.Parse(path.Split('/').First(p => p.StartsWith("DummyCard"))[9..]) - 1;
                var card = cards[index]; card.DisplaySprite.Visible = false; card.GlowSprite.Visible = false;
                var texture = (Texture2D)card.CardMaterial.GetShaderParameter("card_texture");
                var material = new ShaderMaterial { Shader = overlay ? _cardAddShader : _cardShader };
                stage.Materials[path] = material;
                if (overlay) material.SetShaderParameter("tint", Colors.White);
                material.SetShaderParameter("card_texture", texture);
                // 源网格在 XZ 面：右=-X、上=+Z、正面=+Y。按捕获比例扩展四边，保持卡体高度 8.6。
                float height = 8.6f * texture.GetHeight() / card.DisplaySize.Y * 1.12f;
                var mesh = new MeshInstance3D
                {
                    Mesh = new QuadMesh { Size = new Vector2(height * texture.GetWidth() / texture.GetHeight(), height) },
                    Basis = new Basis(Vector3.Left, Vector3.Back, Vector3.Up),
                    Position = new Vector3(0, 0.025f, 0),
                    MaterialOverride = material,
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
                };
                node.AddChild(mesh); if (!overlay) _cards.Add((mesh, material, result)); continue;
            }
            // 卡背、加亮占位卡和矩形背光由动态卡面及同空间轮廓替代。
            if (mn.StartsWith("DummyCard") || mn is "SummonRitualCardAdd" or "ParticlesUnlit" or "ValueMode03" or "CardBack01" or "CardBack02" or "MaskACContAdd01") continue;
            var materialFx = MakeMaterial(mat); stage.Materials[path] = materialFx;
            if (item.TryGetProperty("particle", out var particle))
            {
                int align = (int)F(item, "renderAlignment"); Vector3 pivot = UnityVector(item.GetProperty("pivot"));
                if (F(item, "renderMode") == 4)
                {
                    var mesh = new MeshInstance3D { Mesh = LoadMesh(S(item, "mesh")), MaterialOverride = materialFx, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
                    node.AddChild(mesh); stage.Particles.Add(new Particle(node, mesh, null, particle, path, materialFx, align, pivot));
                }
                else
                {
                    var emitter = new MultiMeshInstance3D
                    {
                        Multimesh = new MultiMesh
                        {
                            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                            UseColors = true,
                            UseCustomData = true,
                            Mesh = new QuadMesh { Size = Vector2.One },
                            InstanceCount = Math.Clamp((int)F(particle.GetProperty("InitialModule"), "maxNumParticles"), 1, 1200),
                            VisibleInstanceCount = 0
                        },
                        MaterialOverride = materialFx,
                        CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
                    };
                    node.AddChild(emitter); emitter.TopLevel = true; emitter.GlobalTransform = Transform3D.Identity;
                    stage.Particles.Add(new Particle(node, null, emitter, particle, path, materialFx, align, pivot));
                }
            }
            else if (S(item, "mesh").Length > 0) node.AddChild(new MeshInstance3D { Mesh = LoadMesh(S(item, "mesh")), MaterialOverride = materialFx, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
        }
        _stages.Add(stage);
    }

    private ShaderMaterial MakeMaterial(JsonElement mat)
    {
        string name = S(mat, "name"), shader = S(mat, "shader");
        var result = new ShaderMaterial
        {
            Shader = shader.Contains("ParticleBlend") ? _blendShader : _shader,
            RenderPriority = Math.Clamp((int)F(mat, "renderQueue") < 0 ? 0 : (int)F(mat, "renderQueue") - 3000, -128, 127)
        };
        var colors = mat.GetProperty("colors"); var floats = mat.GetProperty("floats");
        result.SetShaderParameter("tint", colors.TryGetProperty("_TintColor", out var tint) ? Color(tint) : Colors.White);
        if (mat.GetProperty("textures").TryGetProperty("_MainTex", out var texture) && S(texture, "file").Length > 0)
        {
            result.SetShaderParameter("main_tex", GD.Load<Texture2D>(Root + "assets/" + S(texture, "file")));
            result.SetShaderParameter("uv_scale", new Vector2(F(texture.GetProperty("scale"), "x"), F(texture.GetProperty("scale"), "y")));
        }
        int mode = name == "tlm_RitualFire01" ? 1 : name == "tlmRitualInMesh02" ? 3 : name.Contains("Cylinder") ? 2 : 0;
        result.SetShaderParameter("mode", mode);
        // 参考实机与透明 HDR 舞台对照后的局部亮度补偿，不改变源颜色、轨迹或时间。
        float compensation = name.StartsWith("tlm_RitualCircle") ? 1.5f : name == "EfFusion010" ? 1.6f : 1f;
        result.SetShaderParameter("gain", mode is 1 or 3 ? Math.Max(1, F(floats, "_Emission")) : compensation);
        result.SetShaderParameter("flow_speed", F(floats, "_TimeScale"));
        if (colors.TryGetProperty("_ColorStart", out var start)) result.SetShaderParameter("color_start", Color(start));
        if (colors.TryGetProperty("_ColorEnd", out var end)) result.SetShaderParameter("color_end", Color(end));
        return result;
    }

    private void UpdateStage(Stage stage, float time)
    {
        stage.Root.Visible = time >= 0 && time < F(stage.Data, "duration");
        if (!stage.Root.Visible) return;
        Dictionary<string, float> activeTimes = new() { [""] = time };
        foreach (var track in stage.Data.GetProperty("tracks").EnumerateArray())
        {
            string target = S(track, "target"); float local = time - F(track, "start");
            if (S(track, "kind") == "active")
            {
                bool active = local >= 0 && local < F(track, "duration"); stage.Nodes[target].Visible = active;
                if (active) activeTimes[target] = local; continue;
            }
            float clipTime = Mathf.Clamp(local, 0, F(track, "duration")) * F(track, "speed") + F(track, "clipIn");
            Dictionary<Node3D, Vector3> eulers = []; Dictionary<Node3D, Quaternion> quats = []; Dictionary<Node3D, Vector3> positions = [];
            foreach (var curve in track.GetProperty("curves").EnumerateArray())
            {
                string child = S(curve, "path"), path = target + (target.Length > 0 && child.Length > 0 ? "/" : "") + child;
                if (!stage.Nodes.TryGetValue(path, out var node)) continue;
                string attr = S(curve, "attribute"); float value = Curve(curve.GetProperty("keys"), clipTime);
                if (attr.StartsWith("localEulerAnglesRaw.")) { var v = eulers.GetValueOrDefault(node); v[Axis(attr)] = value; eulers[node] = v; }
                else if (attr.StartsWith("m_LocalRotation.")) { var q = quats.GetValueOrDefault(node, Quaternion.Identity); switch (attr[^1]) { case 'x': q.X = -value; break; case 'y': q.Y = -value; break; case 'z': q.Z = value; break; case 'w': q.W = value; break; } quats[node] = q; }
                else if (attr.StartsWith("m_LocalPosition.")) { var v = positions.GetValueOrDefault(node, node.Position); v[Axis(attr)] = attr[^1] == 'z' ? -value : value; positions[node] = v; }
                else if (attr.StartsWith("m_LocalScale.")) { var v = node.Scale; v[Axis(attr)] = value; node.Scale = v; }
                else if (attr == "m_IsActive") node.Visible = value > 0.5f;
                else if (path == "Black" && attr == "m_Color.a") BackgroundOpacity = Mathf.Clamp(value, 0, 1);
                else if (attr.StartsWith("material.") && stage.Materials.TryGetValue(path, out var material)) SetMaterialCurve(material, attr, value);
            }
            Basis offset = UnityEuler(Vector(track.GetProperty("offsetEuler"))) * UnityEuler(Vector(track.GetProperty("clipEuler")));
            foreach (var pair in eulers) { var scale = pair.Key.Scale; pair.Key.Basis = (pair.Key == stage.Nodes[target] ? offset : Basis.Identity) * UnityEuler(pair.Value); pair.Key.Scale = scale; }
            foreach (var pair in quats) { var scale = pair.Key.Scale; pair.Key.Basis = (pair.Key == stage.Nodes[target] ? offset : Basis.Identity) * new Basis(pair.Value.Normalized()); pair.Key.Scale = scale; }
            foreach (var pair in positions) pair.Key.Position = pair.Key == stage.Nodes[target] ? UnityVector(track.GetProperty("offsetPosition")) + offset * (UnityVector(track.GetProperty("clipPosition")) + pair.Value) : pair.Value;
        }
        foreach (string path in _disabled) if (stage.Nodes.TryGetValue(path, out var disabled)) disabled.Visible = false;
        foreach (var pair in stage.Materials) pair.Value.SetShaderParameter("clock", time);
        foreach (Particle p in stage.Particles)
        {
            string owner = activeTimes.Keys.Where(k => k.Length == 0 || p.Path == k || p.Path.StartsWith(k + "/")).OrderByDescending(k => k.Length).First();
            float age = p.Node.IsVisibleInTree() ? activeTimes[owner] - Scalar(p.Data.GetProperty("startDelay")) : -1;
            if (p.Emitter != null) { UpdateBillboards(p, age); continue; }
            if (p.Mesh == null) continue;
            var initial = p.Data.GetProperty("InitialModule"); float life = Math.Max(0.001f, Scalar(initial.GetProperty("startLifetime")));
            if (age > life && (F(p.Data, "looping") == 1 || Scalar(p.Data.GetProperty("EmissionModule").GetProperty("rateOverTime")) > 0)) age %= life;
            p.Mesh.Visible = age >= 0 && age <= life; if (!p.Mesh.Visible) continue;
            float progress = age / life; float size = Scalar(initial.GetProperty("startSize")); bool separate = F(initial, "size3D") == 1;
            Vector3 scale = new(size, separate ? Scalar(initial.GetProperty("startSizeY")) : size, separate ? Scalar(initial.GetProperty("startSizeZ")) : size);
            var sizes = p.Data.GetProperty("SizeModule"); if (F(sizes, "enabled") == 1) { float x = Scalar(sizes.GetProperty("curve"), progress); bool axes = F(sizes, "separateAxes") == 1; scale *= new Vector3(x, axes ? Scalar(sizes.GetProperty("y"), progress) : x, axes ? Scalar(sizes.GetProperty("z"), progress) : x); }
            p.Mesh.Scale = scale.Max(Vector3.One * 0.0001f); p.Mesh.Position = scale * p.Pivot;
            var color = SampleColor(initial.GetProperty("startColor"), progress, 0.5f); var colors = p.Data.GetProperty("ColorModule");
            if (F(colors, "enabled") == 1) color *= GradientColor(colors.GetProperty("gradient").GetProperty("maxGradient"), progress);
            p.Material.SetShaderParameter("particle_color", color);
        }
    }
    private static Basis UnityEuler(Vector3 degrees) => Basis.FromEuler(new Vector3(-degrees.X, -degrees.Y, degrees.Z) * Mathf.Pi / 180, EulerOrder.Yxz);
    private static void SetMaterialCurve(ShaderMaterial material, string attribute, float value)
    {
        if (attribute.StartsWith("material._TintColor.") || attribute.StartsWith("material._AddColor."))
        {
            string uniform = attribute.StartsWith("material._AddColor.") ? "card_tint" : "tint";
            Color tint = (Color)material.GetShaderParameter(uniform);
            switch (attribute[^1]) { case 'r': tint.R = value; break; case 'g': tint.G = value; break; case 'b': tint.B = value; break; case 'a': tint.A = value; break; }
            material.SetShaderParameter(uniform, tint); return;
        }
        string? parameter = attribute switch { "material.TilingOffset.x" => "tiling_x", "material.TilingOffset.y" => "tiling_y", "material.TilingOffset.z" => "offset_x", "material.TilingOffset.w" => "offset_y", "material._TilingOffset.z" => "offset_x", "material._TilingOffset.w" => "offset_y", "material._TintColor.a" => "material_alpha", _ => null };
        if (parameter != null) material.SetShaderParameter(parameter, value);
    }
    private Mesh LoadMesh(string name)
    {
        if (_meshes.TryGetValue(name, out var existing)) return existing;
        if (!_source.RootElement.GetProperty("coloredMeshes").TryGetProperty(name, out var data)) return _meshes[name] = GD.Load<Mesh>(Root + "assets/" + name);
        var arrays = new Godot.Collections.Array(); arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = data.GetProperty("vertices").EnumerateArray().Select(v => new Vector3(v[0].GetSingle(), v[1].GetSingle(), v[2].GetSingle())).ToArray();
        arrays[(int)Mesh.ArrayType.Normal] = data.GetProperty("normals").EnumerateArray().Select(v => new Vector3(v[0].GetSingle(), v[1].GetSingle(), v[2].GetSingle())).ToArray();
        arrays[(int)Mesh.ArrayType.TexUV] = data.GetProperty("uv").EnumerateArray().Select(v => new Vector2(v[0].GetSingle(), v[1].GetSingle())).ToArray();
        arrays[(int)Mesh.ArrayType.Color] = data.GetProperty("colors").EnumerateArray().Select(v => new Color(v[0].GetSingle(), v[1].GetSingle(), v[2].GetSingle(), v[3].GetSingle())).ToArray();
        arrays[(int)Mesh.ArrayType.Index] = data.GetProperty("indices").EnumerateArray().Select(v => v.GetInt32()).ToArray();
        var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return _meshes[name] = mesh;
    }

    private void UpdateBillboards(Particle particle, float age)
    {
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
        if (age <= length || looping)
        {
            particle.EmissionRemainder += Scalar(emission.GetProperty("rateOverTime"), (looping ? age % length : age) / length) * delta;
            births = (int)particle.EmissionRemainder;
            particle.EmissionRemainder -= births;
        }
        foreach (JsonElement burst in emission.GetProperty("m_Bursts").EnumerateArray())
        {
            float at = F(burst, "time");
            if ((first && at == 0) || (previous < at && age >= at)) births += (int)Scalar(burst.GetProperty("countCurve"));
        }
        births = Math.Min(births, instances.InstanceCount - particle.Live.Count);
        for (int i = 0; i < births; i++)
        {
            int serial = ++particle.Serial;
            float random = RandomUnit(serial, 1), angle = RandomUnit(serial, 2) * Mathf.Tau;
            float birth = first ? 0 : Mathf.Lerp(previous, age, (i + 1f) / Math.Max(1, births));
            float normalized = (looping ? birth % length : birth) / length;
            Vector3 position = Vector3.Zero, direction = Vector3.Up;
            JsonElement shape = data.GetProperty("ShapeModule");
            if (F(shape, "enabled") == 1)
            {
                float radius = F(shape.GetProperty("radius"), "value");
                int type = (int)F(shape, "type");
                if (type == 17)
                {
                    // Unity ParticleSystemShapeType.Donut=17；小环截面在大环周围分散星点。
                    float minorAngle = RandomUnit(serial, 3) * Mathf.Tau;
                    float thickness = Mathf.Clamp(F(shape, "radiusThickness"), 0, 1);
                    float minorRadius = F(shape, "donutRadius") * Mathf.Sqrt(Mathf.Lerp((1 - thickness) * (1 - thickness), 1, RandomUnit(serial, 4)));
                    Vector3 radial = new(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                    direction = radial * Mathf.Cos(minorAngle) + Vector3.Forward * Mathf.Sin(minorAngle);
                    position = radial * radius + direction * minorRadius;
                }
                else
                {
                    direction = type == 10 ? new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0)
                        : new Vector3(Mathf.Cos(angle), RandomUnit(serial, 4) * 2 - 1, Mathf.Sin(angle)).Normalized();
                    float thickness = Mathf.Clamp(F(shape, "radiusThickness"), 0, 1);
                    float inner = 1 - thickness;
                    float radial = type == 10 ? Mathf.Sqrt(Mathf.Lerp(inner * inner, 1, RandomUnit(serial, 3))) : Mathf.Pow(Mathf.Lerp(inner * inner * inner, 1, RandomUnit(serial, 3)), 1f / 3f);
                    position = direction * radius * radial;
                }
                if (shape.TryGetProperty("m_Scale", out var shapeScale)) position *= Vector(shapeScale);
                if (shape.TryGetProperty("m_Rotation", out var shapeRotation))
                {
                    Vector3 angles = Vector(shapeRotation) * Mathf.Pi / 180;
                    Basis basis = Basis.FromEuler(new Vector3(-angles.X, -angles.Y, angles.Z), EulerOrder.Yxz);
                    position = basis * position; direction = basis * direction;
                }
                if (shape.TryGetProperty("m_Position", out var shapePosition)) position += UnityVector(shapePosition);
            }
            Transform3D transform = first ? current : particle.PreviousTransform.InterpolateWith(current, (i + 1f) / Math.Max(1, births));
            Vector3 velocity = direction * Sample(initial.GetProperty("startSpeed"), normalized, random);
            float size = Sample(initial.GetProperty("startSize"), normalized, random);
            float sizeY = F(initial, "size3D") == 1 ? Sample(initial.GetProperty("startSizeY"), normalized, random) : size;
            if (!localSpace) { position = transform * position; velocity = transform.Basis * velocity; }
            Vector3 scaling = transform.Basis.Scale.Abs();
            float rotation = initial.TryGetProperty("startRotation", out var initialRotation) ? Sample(initialRotation, normalized, random) : 0;
            JsonElement spinModule = data.GetProperty("RotationModule");
            float spin = F(spinModule, "enabled") == 1 ? Scalar(spinModule.GetProperty("curve")) : 0;
            Color color = SampleColor(initial.GetProperty("startColor"), normalized, random);
            particle.Live.Add(new Billboard(birth, Math.Max(0.01f, Sample(initial.GetProperty("startLifetime"), normalized, random)),
                position, velocity, new Vector2(size * scaling.X, sizeY * scaling.Y), rotation, spin, color, random));
        }
        JsonElement sizes = data.GetProperty("SizeModule"), colors = data.GetProperty("ColorModule");
        int index = 0;
        foreach (Billboard p in particle.Live)
        {
            float lifetime = age - p.Birth, normalized = Mathf.Clamp(lifetime / p.Lifetime, 0, 1);
            Vector2 size = p.Size;
            if (F(sizes, "enabled") == 1)
            {
                float x = Sample(sizes.GetProperty("curve"), normalized, p.Random);
                size *= new Vector2(x, F(sizes, "separateAxes") == 1 ? Sample(sizes.GetProperty("y"), normalized, p.Random) : x);
            }
            Color color = p.Color;
            if (F(colors, "enabled") == 1) color *= GradientColor(colors.GetProperty("gradient").GetProperty("maxGradient"), normalized);
            Vector3 position = p.Position + p.Velocity * lifetime;
            if (localSpace) position = current * position;
            Basis facing = particle.Alignment == 2 ? current.Basis.Orthonormalized() : _camera.GlobalBasis;
            Basis basis = facing * new Basis(Vector3.Back, p.Rotation + p.Spin * lifetime);
            basis = basis.ScaledLocal(new Vector3(Math.Max(size.X, 0.0001f), Math.Max(size.Y, 0.0001f), 1));
            position += basis * particle.Pivot;
            instances.SetInstanceTransform(index, new Transform3D(basis, position));
            instances.SetInstanceColor(index, color);
            var sheet = data.GetProperty("UVModule");
            int tilesX = 1, tilesY = 1, frame = 0;
            if (F(sheet, "enabled") == 1)
            {
                tilesX = Math.Max(1, (int)F(sheet, "tilesX")); tilesY = Math.Max(1, (int)F(sheet, "tilesY"));
                float sheetTime = normalized * Math.Max(1, F(sheet, "cycles"));
                float frameValue = Sample(sheet.GetProperty("frameOverTime"), sheetTime, p.Random) + Scalar(sheet.GetProperty("startFrame"));
                frame = (int)(frameValue * tilesX * tilesY) % (tilesX * tilesY);
            }
            instances.SetInstanceCustomData(index++, new Color(frame, tilesX, tilesY, 0));
        }
        instances.VisibleInstanceCount = index;
        particle.PreviousAge = age;
        particle.PreviousTransform = current;
    }

    private static float RandomUnit(int serial, int channel)
    {
        uint n = unchecked((uint)(serial * 747796405 + channel * 289133645));
        n = ((n >> ((int)(n >> 28) + 4)) ^ n) * 277803737u;
        return ((n >> 22) ^ n) / (float)uint.MaxValue;
    }
    private static float Sample(JsonElement value, float time, float random)
    {
        int mode = (int)F(value, "minMaxState");
        float maximum = Scalar(value, time);
        if (mode == 3) return Mathf.Lerp(F(value, "minScalar"), maximum, random);
        if (mode == 2) return Mathf.Lerp(F(value, "minScalar") * ObjectCurve(value.GetProperty("minCurve").GetProperty("m_Curve"), time), maximum, random);
        return maximum;
    }
    private static Color SampleColor(JsonElement value, float time, float random) => (int)F(value, "minMaxState") switch
    {
        1 => GradientColor(value.GetProperty("maxGradient"), time),
        2 => Color(value.GetProperty("minColor")).Lerp(Color(value.GetProperty("maxColor")), random),
        3 => GradientColor(value.GetProperty("minGradient"), time).Lerp(GradientColor(value.GetProperty("maxGradient"), time), random),
        4 => GradientColor(value.GetProperty("maxGradient"), random),
        _ => Color(value.GetProperty("maxColor"))
    };
    private static Color GradientColor(JsonElement gradient, float time)
    {
        int count = (int)F(gradient, "m_NumColorKeys");
        Color previous = Color(gradient.GetProperty("key0"));
        float previousTime = F(gradient, "ctime0") / 65535f;
        for (int i = 1; i < count; i++)
        {
            float nextTime = F(gradient, "ctime" + i) / 65535f;
            Color next = Color(gradient.GetProperty("key" + i));
            if (time <= nextTime)
            {
                if (F(gradient, "m_Mode") != 1) previous = previous.Lerp(next, Mathf.Clamp((time - previousTime) / Math.Max(0.0001f, nextTime - previousTime), 0, 1));
                break;
            }
            previous = next; previousTime = nextTime;
        }
        previous.A = GradientAlpha(gradient, time);
        return previous;
    }

    private static float Scalar(JsonElement value, float time = 0)
    {
        float scalar = F(value, "scalar");
        if ((int)F(value, "minMaxState") is 1 or 2) return scalar * ObjectCurve(value.GetProperty("maxCurve").GetProperty("m_Curve"), time);
        return scalar;
    }
    private static float ObjectCurve(JsonElement keys, float time) => Evaluate(keys.EnumerateArray().Select(k => new Key(F(k, "time"), F(k, "value"), Slope(k, "inSlope"), Slope(k, "outSlope"))).ToArray(), time);
    private static float Curve(JsonElement keys, float time) => Evaluate(keys.EnumerateArray().Select(k => new Key(k[0].GetSingle(), k[1].GetSingle(), k[2].ValueKind == JsonValueKind.Number ? k[2].GetSingle() : float.PositiveInfinity, k[3].ValueKind == JsonValueKind.Number ? k[3].GetSingle() : float.PositiveInfinity)).ToArray(), time);
    private static float Evaluate(Key[] keys, float time)
    {
        if (keys.Length == 0) return 1;
        if (time <= keys[0].Time) return keys[0].Value;
        for (int i = 1; i < keys.Length; i++)
        {
            Key a = keys[i - 1], b = keys[i];
            if (time > b.Time) continue;
            if (!float.IsFinite(a.Out) || !float.IsFinite(b.In)) return a.Value;
            float dt = b.Time - a.Time, t = (time - a.Time) / dt, t2 = t * t, t3 = t2 * t;
            return (2 * t3 - 3 * t2 + 1) * a.Value + (t3 - 2 * t2 + t) * dt * a.Out + (-2 * t3 + 3 * t2) * b.Value + (t3 - t2) * dt * b.In;
        }
        return keys[^1].Value;
    }
    private static float GradientAlpha(JsonElement gradient, float time)
    {
        int count = (int)F(gradient, "m_NumAlphaKeys");
        float previousTime = 0, previous = F(gradient.GetProperty("key0"), "a");
        for (int i = 1; i < count; i++)
        {
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
    private static Vector3 Vector(JsonElement e) => new(F(e, "x"), F(e, "y"), F(e, "z"));
    private static Vector3 UnityVector(JsonElement e) => new(F(e, "x"), F(e, "y"), -F(e, "z"));
    private static Color Color(JsonElement e) => new(F(e, "r"), F(e, "g"), F(e, "b"), F(e, "a"));
    private sealed record Key(float Time, float Value, float In, float Out);
    private sealed record Stage(Node3D Root, JsonElement Data, float Start)
    {
        public Dictionary<string, Node3D> Nodes { get; } = [];
        public Dictionary<string, ShaderMaterial> Materials { get; } = [];
        public List<Particle> Particles { get; } = [];
    }
    private sealed record Particle(Node3D Node, MeshInstance3D? Mesh, MultiMeshInstance3D? Emitter, JsonElement Data, string Path, ShaderMaterial Material, int Alignment, Vector3 Pivot)
    {
        public bool? Looping { get; set; }
        public float PreviousAge { get; set; } = -1;
        public float EmissionRemainder { get; set; }
        public int Serial { get; set; }
        public Transform3D PreviousTransform { get; set; }
        public List<Billboard> Live { get; } = [];
    }
    private sealed record Billboard(float Birth, float Lifetime, Vector3 Position, Vector3 Velocity, Vector2 Size, float Rotation, float Spin, Color Color, float Random);
}
