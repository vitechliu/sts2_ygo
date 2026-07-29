using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace VYgo.Core.Effects;

/// <summary>
/// Three-stage 3D Xyz summon presentation, rendered into the owning SubViewport.
/// The source motion curves mirror the Unity timelines:
/// SummonXYZMain00 (2.4167s), SummonXYZExplosion01 (1.6833s),
/// and SummonXYZPostXYZ (1.3333s). The result stages are time-compressed so
/// Explosion + PostXYZ + fade finish within one second of the result-card reveal.
/// </summary>
public partial class NXyzSummonManager : Node3D {
    public const float MainDuration = 2.416667f;
    private const float ExplosionSourceDuration = 1.683333f;
    private const float PostSourceDuration = 1.333333f;
    public const float ExplosionDuration = 0.55f;
    public const float PostDuration = 0.3f;
    private const float ExplosionPlaybackSpeed =
        ExplosionSourceDuration / ExplosionDuration;
    private const float PostPlaybackSpeed =
        PostSourceDuration / PostDuration;

    private static readonly Color XyzCyan = new("72e9ff");
    private static readonly Color XyzBlue = new("328dff");
    private static readonly Color XyzViolet = new("7957ff");
    private static readonly Color XyzFormationGreen = new("b8ffd0");
    private static readonly Color MaterialYellow = new("ffe65b");

    private readonly List<StandardMaterial3D> _liveMaterials = [];
    private Node3D _stageRoot = null!;
    private Camera3D _camera = null!;

    private Texture2D _starTexture = null!;
    private Texture2D _orbTexture = null!;
    private Texture2D _starrySkyTexture = null!;
    private Texture2D _swirlTexture = null!;
    private Texture2D _vortexTexture = null!;
    private Texture2D _flareTexture = null!;
    private Texture2D _cardTrailTexture = null!;

    public override void _Ready() {
        base._Ready();
        _camera = GetNode<Camera3D>("Camera3D");
        _stageRoot = GetNode<Node3D>("StageRoot");

        _starTexture = LoadTexture("flare006.png");
        _orbTexture = LoadTexture("EFF_flrcmn_01a_TEX.png");
        _starrySkyTexture = LoadTexture("StarrySky001.png");
        _swirlTexture = LoadTexture("Swirl001.png");
        _vortexTexture = LoadTexture("MaterialUzu01.png");
        _flareTexture = LoadTexture("CenterFlare01.png");
        _cardTrailTexture = LoadTexture("CardTrail02.png");

        _camera.Current = true;
        _camera.Fov = 46f;
        _camera.Near = 0.05f;
        _camera.Far = 160f;
        _camera.Position = new Vector3(0f, 1.2f, 38f);
        _camera.LookAt(Vector3.Zero, Vector3.Up);
        ClearStage();
    }

    public async Task PlayMain(int visibleMaterialCount) {
        ClearStage();

        FxMesh backdrop = CreateQuad(
            _starrySkyTexture,
            GetViewportCoverSize(-13f, 1.32f),
            new Color(XyzBlue, 0f),
            new Vector3(0f, 0f, -13f)
        );
        FxMesh vortexBack = CreateQuad(
            _vortexTexture,
            new Vector2(24f, 24f),
            new Color(XyzViolet, 0f),
            new Vector3(0f, 0f, -7.5f)
        );
        FxMesh vortexFront = CreateQuad(
            _swirlTexture,
            new Vector2(17f, 17f),
            new Color(XyzBlue, 0f),
            new Vector3(0f, 0f, -5.5f)
        );
        SphereMesh holeCoreMesh = new() {
            Radius = 2.65f,
            Height = 5.3f,
            RadialSegments = 48,
            Rings = 24
        };
        FxMesh holeCore = CreateMesh(
            holeCoreMesh,
            new Color(Colors.Black, 0f),
            new Vector3(0f, 0f, -4.7f),
            additive: false
        );

        int starCount = Math.Clamp(Math.Max(9, visibleMaterialCount * 3), 9, 18);
        List<StarCluster> stars = [];
        for (int i = 0; i < starCount; i++) {
            Node3D root = new() { Name = $"MaterialStar{i + 1:00}" };
            _stageRoot.AddChild(root);

            FxMesh core = CreateQuad(
                _orbTexture,
                new Vector2(1.9f, 1.9f),
                new Color(MaterialYellow, 0.95f),
                Vector3.Zero,
                parent: root
            );
            GpuParticles3D trail = CreateTrailParticles(
                root,
                MaterialYellow,
                _orbTexture,
                amount: 26,
                lifetime: 0.62f,
                size: new Vector2(0.9f, 0.9f)
            );
            stars.Add(new StarCluster(root, core, trail));
        }

        await AnimateFor(MainDuration, (elapsed, progress) => {
            float intro = SmoothStep(Mathf.Clamp(elapsed / 0.42f, 0f, 1f));
            SetAlpha(backdrop, 0.56f * intro);
            SetRotationZ(backdrop.Node, elapsed * 0.045f);

            float holeProgress = SmoothStep(Mathf.Clamp((elapsed - 0.85f) / 0.32f, 0f, 1f));
            SetAlpha(vortexBack, 0.72f * holeProgress);
            SetAlpha(vortexFront, 0.88f * holeProgress);
            SetAlpha(holeCore, 0.98f * holeProgress);
            vortexBack.Node.Scale = Vector3.One * Mathf.Lerp(0.28f, 1.08f, holeProgress);
            vortexFront.Node.Scale = Vector3.One * Mathf.Lerp(0.18f, 1f, holeProgress);
            holeCore.Node.Scale = Vector3.One * Mathf.Lerp(0.12f, 1f, holeProgress);
            SetRotationZ(vortexBack.Node, elapsed * 1.55f);
            SetRotationZ(vortexFront.Node, -elapsed * 2.25f);

            float collapse = SmoothStep(Mathf.Clamp((progress - 0.08f) / 0.92f, 0f, 1f));
            for (int i = 0; i < stars.Count; i++) {
                StarCluster star = stars[i];
                int armIndex = i % 3;
                float armOffset = armIndex * Mathf.Tau / 3f;
                float lane = i / 3f;
                float startingRadius = 11.5f + lane * 1.55f;
                float turns = 2.05f + armIndex * 0.12f;
                float angle = armOffset + lane * 0.36f - collapse * Mathf.Tau * turns;
                float radius = Mathf.Lerp(startingRadius, 1.15f, Mathf.Pow(collapse, 1.12f));
                float depth = Mathf.Lerp(4.8f - lane * 0.7f, 0f, collapse);
                star.Root.Position = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius * 0.57f,
                    depth + Mathf.Sin(angle * 1.7f) * 1.25f
                );
                float pulse = 0.82f + Mathf.Sin(elapsed * 12f + i) * 0.16f;
                star.Core.Node.Scale = Vector3.One * pulse;
                SetAlpha(star.Core, Mathf.Lerp(0.95f, 0.2f, Mathf.Pow(collapse, 4f)));
            }
        });

        foreach (StarCluster star in stars) {
            star.Trail.Emitting = false;
        }
    }

    public async Task PlayExplosion() {
        ClearStage();

        FxMesh centerFlare = CreateQuad(
            _flareTexture,
            new Vector2(4.6f, 4.6f),
            new Color(Colors.White, 0f),
            new Vector3(0f, 0f, -0.6f)
        );
        RitualRing meridianRing =
            CreateRitualRing(
                6.35f,
                new Vector3(76f, -26f, 0f),
                -0.9f,
                0.12f
            );

        List<RitualRing> formationRings = [
            CreateRitualRing(7.1f, new Vector3(90f, 0f, 0f), -1.65f, 0f),
            CreateRitualRing(4.85f, new Vector3(90f, 0f, 0f), -1.42f, 0.035f),
            CreateRitualRing(2.65f, new Vector3(90f, 0f, 0f), -1.18f, 0.07f),
            meridianRing
        ];

        float[] rayAngles = [-158f, -137f, -116f, -25f, 12f];
        List<FormationRay> formationRays = [];
        for (int i = 0; i < rayAngles.Length; i++) {
            float radians = Mathf.DegToRad(rayAngles[i]);
            float length = i == 3 ? 8.4f : 6.2f + i * 0.42f;
            FxMesh ray = CreateFormationRay(
                radians,
                length,
                i == 3 ? 0.78f : 0.52f,
                i == 3 ? new Color("ffffd0") : new Color("efffb9"),
                -0.72f + i * 0.025f
            );
            formationRays.Add(new FormationRay(ray, i * 0.025f));
        }

        List<RadiantRing> rings = [];
        for (int i = 0; i < 18; i++) {
            float angle = Mathf.Tau * i / 18f;
            Vector3 direction = new(
                Mathf.Cos(angle),
                Mathf.Sin(angle) * 0.68f,
                Mathf.Sin(angle * 2.7f) * 0.48f
            );
            FxMesh ring = CreateRing(
                i % 3 == 0 ? Colors.White : XyzCyan,
                new Vector3(0f, 0f, -0.2f + (i % 4) * 0.35f)
            );
            ring.Node.RotationDegrees = new Vector3(
                72f + (i % 3) * 17f,
                i * 31f,
                i * 13f
            );
            rings.Add(new RadiantRing(ring, direction.Normalized(), i * 0.035f));
        }

        GpuParticles3D flashParticles = CreateBurstParticles(
            _stageRoot,
            XyzCyan,
            _starTexture,
            amount: 84,
            lifetime: 0.9f / ExplosionPlaybackSpeed,
            velocityMin: 5f * ExplosionPlaybackSpeed,
            velocityMax: 18f * ExplosionPlaybackSpeed,
            sizeMin: 0.16f,
            sizeMax: 0.62f
        );
        flashParticles.Emitting = true;

        await AnimateFor(ExplosionDuration, (elapsed, _) => {
            float timelineElapsed = elapsed * ExplosionPlaybackSpeed;
            float columnT = SmoothStep(Mathf.Clamp(timelineElapsed / 0.9f, 0f, 1f));
            centerFlare.Node.Scale = Vector3.One * Mathf.Lerp(0.08f, 1.55f, columnT);
            SetAlpha(centerFlare, Mathf.Sin(columnT * Mathf.Pi) * 0.38f);

            for (int i = 0; i < formationRings.Count; i++) {
                RitualRing ring = formationRings[i];
                float ringT = SmoothStep(Mathf.Clamp(
                    (timelineElapsed - 0.14f - ring.Delay) / 0.46f,
                    0f,
                    1f
                ));
                float fade = 1f - SmoothStep(Mathf.Clamp(
                    (timelineElapsed - 0.92f - ring.Delay * 0.3f) / 0.56f,
                    0f,
                    1f
                ));
                float scale = ring.Radius * Mathf.Lerp(0.2f, 1f, ringT);
                ring.Core.Node.Scale = Vector3.One * scale;
                ring.Glow.Node.Scale = Vector3.One * scale;
                SetAlpha(ring.Core, ringT * fade * (i == 3 ? 0.42f : 0.58f));
                SetAlpha(ring.Glow, ringT * fade * (i == 3 ? 0.18f : 0.27f));
            }

            for (int i = 0; i < formationRays.Count; i++) {
                FormationRay ray = formationRays[i];
                float rayT = SmoothStep(Mathf.Clamp(
                    (timelineElapsed - 0.24f - ray.Delay) / 0.34f,
                    0f,
                    1f
                ));
                float fade = 1f - SmoothStep(Mathf.Clamp(
                    (timelineElapsed - 0.78f - ray.Delay) / 0.5f,
                    0f,
                    1f
                ));
                ray.Mesh.Node.Scale = new Vector3(
                    Mathf.Lerp(0.12f, 1f, rayT),
                    Mathf.Lerp(0.35f, 1f, rayT),
                    1f
                );
                SetAlpha(ray.Mesh, rayT * fade * (i == 3 ? 0.95f : 0.76f));
            }

            foreach (RadiantRing radiantRing in rings) {
                float ringT = SmoothStep(Mathf.Clamp(
                    (timelineElapsed - 0.566667f - radiantRing.Delay) / 0.72f,
                    0f,
                    1f
                ));
                radiantRing.Mesh.Node.Position =
                    radiantRing.Direction * Mathf.Lerp(0.15f, 17.5f, ringT);
                radiantRing.Mesh.Node.Scale =
                    Vector3.One * Mathf.Lerp(0.05f, 1.7f, ringT);
                SetRotationZ(
                    radiantRing.Mesh.Node,
                    radiantRing.Mesh.Node.Rotation.Z
                    + 0.035f * ExplosionPlaybackSpeed
                );
                SetAlpha(radiantRing.Mesh, Mathf.Sin(ringT * Mathf.Pi) * 0.72f);
            }
        });
    }

    public async Task PlayPostXyz() {
        ClearStage();

        FxMesh flash = CreateQuad(
            _flareTexture,
            new Vector2(10f, 10f),
            new Color(Colors.White, 0f),
            new Vector3(0f, 0f, -2.6f)
        );
        List<FxMesh> postRings = [];
        for (int i = 0; i < 5; i++) {
            FxMesh ring = CreateRing(
                i % 2 == 0 ? Colors.White : XyzCyan,
                new Vector3(0f, 0f, -1.8f - i * 0.35f)
            );
            ring.Node.RotationDegrees = new Vector3(90f, i * 17f, i * 29f);
            postRings.Add(ring);
        }

        GpuParticles3D cardParticles = CreateBurstParticles(
            _stageRoot,
            MaterialYellow,
            _starTexture,
            amount: 132,
            lifetime: 1.05f / PostPlaybackSpeed,
            velocityMin: 11f * PostPlaybackSpeed,
            velocityMax: 32f * PostPlaybackSpeed,
            sizeMin: 0.12f,
            sizeMax: 0.52f
        );
        GpuParticles3D streakParticles = CreateBurstParticles(
            _stageRoot,
            XyzCyan,
            _cardTrailTexture,
            amount: 38,
            lifetime: 0.82f / PostPlaybackSpeed,
            velocityMin: 13f * PostPlaybackSpeed,
            velocityMax: 30f * PostPlaybackSpeed,
            sizeMin: 0.12f,
            sizeMax: 0.52f,
            particleSize: new Vector2(0.16f, 0.88f)
        );

        await AnimateFor(PostDuration, (elapsed, _) => {
            float timelineElapsed = elapsed * PostPlaybackSpeed;
            if (timelineElapsed >= 0.166667f && !cardParticles.Emitting) {
                cardParticles.Emitting = true;
                streakParticles.Emitting = true;
            }

            float burstT = SmoothStep(Mathf.Clamp(
                (timelineElapsed - 0.12f) / 0.82f,
                0f,
                1f
            ));
            flash.Node.Scale = Vector3.One * Mathf.Lerp(0.12f, 3.25f, burstT);
            SetAlpha(flash, Mathf.Sin(burstT * Mathf.Pi) * 0.62f);

            for (int i = 0; i < postRings.Count; i++) {
                float ringT = SmoothStep(Mathf.Clamp(
                    (timelineElapsed - 0.16f - i * 0.055f) / 0.88f,
                    0f,
                    1f
                ));
                FxMesh ring = postRings[i];
                ring.Node.Scale = Vector3.One * Mathf.Lerp(0.04f, 6.8f + i * 0.95f, ringT);
                ring.Node.Position = new Vector3(
                    Mathf.Sin(i * 2.4f) * ringT * 2f,
                    Mathf.Cos(i * 1.8f) * ringT * 1.25f,
                    -1.8f - i * 0.35f
                );
                SetAlpha(ring, Mathf.Sin(ringT * Mathf.Pi) * 0.58f);
            }
        });
    }

    public async Task FadeOut() {
        List<(StandardMaterial3D Material, Color Color)> materials = _liveMaterials
            .Where(GodotObject.IsInstanceValid)
            .Select(material => (material, material.AlbedoColor))
            .ToList();

        await AnimateFor(0.08f, (_, progress) => {
            foreach ((StandardMaterial3D material, Color color) in materials) {
                material.AlbedoColor = color with {
                    A = Mathf.Lerp(color.A, 0f, progress)
                };
            }
        });
    }

    private FxMesh CreateQuad(
        Texture2D texture,
        Vector2 size,
        Color color,
        Vector3 position,
        bool additive = true,
        Node3D? parent = null
    ) {
        QuadMesh mesh = new() { Size = size };
        return CreateMesh(mesh, color, position, additive, texture, parent);
    }

    private FxMesh CreateRing(Color color, Vector3 position) {
        TorusMesh mesh = new() {
            InnerRadius = 0.86f,
            OuterRadius = 1f,
            Rings = 48,
            RingSegments = 8
        };
        return CreateMesh(mesh, new Color(color, 0f), position);
    }

    private RitualRing CreateRitualRing(
        float radius,
        Vector3 rotationDegrees,
        float z,
        float delay
    ) {
        FxMesh glow = CreateRing(
            XyzFormationGreen,
            new Vector3(0f, 0f, z),
            innerRadius: 0.87f
        );
        FxMesh core = CreateRing(
            new Color("72ff9e"),
            new Vector3(0f, 0f, z - 0.015f),
            innerRadius: 0.952f
        );
        glow.Node.RotationDegrees = rotationDegrees;
        core.Node.RotationDegrees = rotationDegrees;
        return new RitualRing(core, glow, radius, delay);
    }

    private FxMesh CreateRing(
        Color color,
        Vector3 position,
        float innerRadius
    ) {
        TorusMesh mesh = new() {
            InnerRadius = innerRadius,
            OuterRadius = 1f,
            Rings = 64,
            RingSegments = 10
        };
        return CreateMesh(mesh, new Color(color, 0f), position);
    }

    private FxMesh CreateFormationRay(
        float angle,
        float length,
        float endWidth,
        Color color,
        float z
    ) {
        Vector2 direction = Vector2.FromAngle(angle);
        Vector2 normal = new(-direction.Y, direction.X);
        Vector2 end = direction * length;

        SurfaceTool surface = new();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        surface.SetColor(new Color(Colors.White, 0.95f));
        surface.AddVertex(new Vector3(0f, 0f, z));
        surface.SetColor(new Color(Colors.White, 0.08f));
        Vector2 endLeft = end + normal * endWidth;
        surface.AddVertex(new Vector3(endLeft.X, endLeft.Y, z));
        surface.SetColor(new Color(Colors.White, 0.08f));
        Vector2 endRight = end - normal * endWidth;
        surface.AddVertex(new Vector3(endRight.X, endRight.Y, z));
        return CreateMesh(
            surface.Commit(),
            new Color(color, 0f),
            Vector3.Zero
        );
    }

    private FxMesh CreateMesh(
        Mesh mesh,
        Color color,
        Vector3 position,
        bool additive = true,
        Texture2D? texture = null,
        Node3D? parent = null
    ) {
        StandardMaterial3D material = CreateMaterial(color, texture, additive);
        MeshInstance3D node = new() {
            Mesh = mesh,
            MaterialOverride = material,
            Position = position,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        (parent ?? _stageRoot).AddChild(node);
        return new FxMesh(node, material);
    }

    private StandardMaterial3D CreateMaterial(
        Color color,
        Texture2D? texture = null,
        bool additive = true,
        bool billboard = false
    ) {
        StandardMaterial3D material = new() {
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            BlendMode = additive
                ? BaseMaterial3D.BlendModeEnum.Add
                : BaseMaterial3D.BlendModeEnum.Mix,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            AlbedoColor = color,
            AlbedoTexture = texture,
            VertexColorUseAsAlbedo = true,
            BillboardMode = billboard
                ? BaseMaterial3D.BillboardModeEnum.Enabled
                : BaseMaterial3D.BillboardModeEnum.Disabled
        };
        _liveMaterials.Add(material);
        return material;
    }

    private GpuParticles3D CreateTrailParticles(
        Node3D parent,
        Color color,
        Texture2D texture,
        int amount,
        float lifetime,
        Vector2 size
    ) {
        StandardMaterial3D material = CreateMaterial(
            new Color(color, 0.78f),
            texture,
            billboard: true
        );
        QuadMesh quad = new() {
            Size = size,
            Material = material
        };
        ParticleProcessMaterial process = new() {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Point,
            Direction = Vector3.Zero,
            Spread = 0f,
            InitialVelocityMin = 0f,
            InitialVelocityMax = 0f,
            Gravity = Vector3.Zero,
            ScaleMin = 0.42f,
            ScaleMax = 1.2f,
            Color = Colors.White,
            ColorRamp = CreateParticleFadeRamp(color)
        };
        GpuParticles3D particles = new() {
            Amount = amount,
            Lifetime = lifetime,
            OneShot = false,
            Randomness = 0.35f,
            FixedFps = 60,
            LocalCoords = false,
            ProcessMaterial = process,
            DrawPass1 = quad,
            Emitting = true
        };
        parent.AddChild(particles);
        return particles;
    }

    private GpuParticles3D CreateBurstParticles(
        Node3D parent,
        Color color,
        Texture2D texture,
        int amount,
        float lifetime,
        float velocityMin,
        float velocityMax,
        float sizeMin,
        float sizeMax,
        Vector2? particleSize = null
    ) {
        StandardMaterial3D material = CreateMaterial(
            new Color(color, 0.92f),
            texture,
            billboard: true
        );
        QuadMesh quad = new() {
            Size = particleSize ?? new Vector2(0.58f, 0.58f),
            Material = material
        };
        ParticleProcessMaterial process = new() {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
            EmissionSphereRadius = 0.3f,
            Direction = new Vector3(0f, 0f, 1f),
            Spread = 180f,
            InitialVelocityMin = velocityMin,
            InitialVelocityMax = velocityMax,
            Gravity = Vector3.Zero,
            ScaleMin = sizeMin,
            ScaleMax = sizeMax,
            Color = Colors.White,
            ColorRamp = CreateParticleFadeRamp(color)
        };
        GpuParticles3D particles = new() {
            Amount = amount,
            Lifetime = lifetime,
            OneShot = true,
            Explosiveness = 0.94f,
            Randomness = 0.62f,
            FixedFps = 60,
            LocalCoords = false,
            ProcessMaterial = process,
            DrawPass1 = quad,
            Emitting = false
        };
        parent.AddChild(particles);
        return particles;
    }

    private static GradientTexture1D CreateParticleFadeRamp(Color color) {
        Gradient gradient = new() {
            Offsets = [0f, 0.12f, 0.68f, 1f],
            Colors = [
                new Color(color, 0f),
                new Color(color, 1f),
                new Color(color, 0.72f),
                new Color(color, 0f)
            ]
        };
        return new GradientTexture1D { Gradient = gradient };
    }

    private async Task AnimateFor(float duration, Action<float, float> update) {
        float elapsed = 0f;
        update(0f, 0f);
        while (elapsed < duration
               && GodotObject.IsInstanceValid(this)
               && IsInsideTree()) {
            await this.AwaitProcessFrame();
            elapsed = Math.Min(duration, elapsed + (float)GetProcessDeltaTime());
            update(elapsed, SmoothStep(elapsed / duration));
        }
    }

    private void ClearStage() {
        if (_stageRoot == null) return;
        foreach (Node child in _stageRoot.GetChildren()) {
            child.QueueFree();
        }
        _liveMaterials.Clear();
    }

    private static void SetAlpha(FxMesh mesh, float alpha) {
        Color color = mesh.Material.AlbedoColor;
        mesh.Material.AlbedoColor = color with { A = Mathf.Clamp(alpha, 0f, 1f) };
    }

    private static void SetRotationZ(Node3D node, float value) {
        Vector3 rotation = node.Rotation;
        rotation.Z = value;
        node.Rotation = rotation;
    }

    private Texture2D LoadTexture(string filename) {
        return GD.Load<Texture2D>(AssetPath(filename));
    }

    private Vector2 GetViewportCoverSize(float planeZ, float overscan) {
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        float aspect = viewportSize.Y > 0f
            ? viewportSize.X / viewportSize.Y
            : 16f / 9f;
        float distance = Mathf.Abs(_camera.Position.Z - planeZ);
        float height = 2f
            * distance
            * Mathf.Tan(Mathf.DegToRad(_camera.Fov * 0.5f))
            * overscan;
        return new Vector2(height * aspect, height);
    }

    private static string AssetPath(string filename) {
        return $"res://VYgo/scenes/summon/xyz/assets/{filename}";
    }

    private static float SmoothStep(float value) {
        value = Mathf.Clamp(value, 0f, 1f);
        return value * value * (3f - 2f * value);
    }

    private sealed record FxMesh(MeshInstance3D Node, StandardMaterial3D Material);
    private sealed record StarCluster(Node3D Root, FxMesh Core, GpuParticles3D Trail);
    private sealed record RitualRing(
        FxMesh Core,
        FxMesh Glow,
        float Radius,
        float Delay
    );
    private sealed record FormationRay(FxMesh Mesh, float Delay);
    private sealed record RadiantRing(FxMesh Mesh, Vector3 Direction, float Delay);
}
