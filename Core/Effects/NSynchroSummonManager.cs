using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using VYgo.Scripts;
using VYgo.Utils;

namespace VYgo.Core.Effects;

/// <summary>
/// Unity SummonSynchro01/02 的 Godot 运行时播放器。
/// 时间点保持源 Timeline：环 1.333、等级转换 1.850、Post 3.250、
/// 结果卡按实机观感提前至 3.708 开始，StartCard 4.600、结束 5.017 秒。
/// </summary>
public partial class NSynchroSummonManager : Node3D {
    public const float MainDuration = 5.016667f;
    public const float NoMaterialDuration = 3.933333f;
    public const float RingStart = 1.333333f;
    public const float LevelStart = 1.85f;
    public const float StarEnd = 2.683333f;
    public const float PostStart = 3.25f;
    // 光环在 3.333 秒翻正；将原先 0.75 秒的空等候缩短一半。
    public const float StrongSummon = 3.708333f;
    public const float StartCard = 4.6f;
    public float TimelineElapsed { get; private set; }

    private static readonly Color TunerCyan = new("16e6dc");
    private static readonly Color TunerCyanBright = new("5cfff6");
    private static readonly Color TunerCyanDeep = new("00aebf");

    private readonly List<ShaderMaterial> _materials = [];
    private Node3D _stageRoot = null!;
    private Camera3D _camera = null!;
    private Shader _shader = null!;
    private Shader _solidShader = null!;
    private Texture2D _flareTexture = null!;
    private Texture2D _starTexture = null!;

    public override void _Ready() {
        base._Ready();
        _camera = GetNode<Camera3D>("Camera3D");
        _stageRoot = GetNode<Node3D>("StageRoot");
        _shader = GD.Load<Shader>("res://VYgo/scenes/summon/synchro/synchro_fx.gdshader");
        _solidShader = GD.Load<Shader>(
            "res://VYgo/scenes/summon/synchro/synchro_ring.gdshader"
        );
        if (GD.Load<AnimationLibrary>(
                "res://VYgo/scenes/summon/synchro/synchro_timeline_library.tres"
            ) == null) {
            Entry.Logger.Error("Failed to load audited Synchro AnimationLibrary.");
        }
        _flareTexture = LoadTexture("flare00_00.png");
        _starTexture = LoadTexture("SynchroStar01.png");
        _camera.Current = true;
        // Unity 源相机为 orthographicSize=5。Godot Size 表示完整视口高度，故对应 10。
        // 旧版透视相机会让不同深度的圆环发生尺寸和中心漂移。
        _camera.Projection = Camera3D.ProjectionType.Orthogonal;
        _camera.Size = 10f;
        _camera.KeepAspect = Camera3D.KeepAspectEnum.Height;
        _camera.Near = 0.05f;
        _camera.Far = 180f;
        // 网格先换算到相机空间，避免重复套用 Unity 场景的 70 度倾角。
        _camera.Position = new Vector3(0f, 0f, 40f);
        _camera.LookAt(Vector3.Zero, Vector3.Up);
        ClearStage();
    }

    public async Task PlayMain(int targetLevel, int tunerLevel, int nonTunerLevel) {
        ClearStage();
        TimelineElapsed = 0f;
        int circleVariant = targetLevel < 5 ? 1 : targetLevel <= 8 ? 2 : 3;
        Stage stage = BuildMainStage(circleVariant);
        bool playedIntro = false;
        bool playedLevel = false;
        bool playedCircleA = false;
        bool playedCircleB = false;
        bool playedPost = false;

        await AnimateFor(MainDuration, (elapsed, _) => {
            TimelineElapsed = elapsed;
            if (!playedIntro && elapsed >= 1.316667f) {
                playedIntro = true;
                SFXUtil.Play("event:/vygo/sfx/synchro_01_01");
            }
            if (!playedLevel && elapsed >= LevelStart) {
                playedLevel = true;
                SFXUtil.Play("event:/vygo/sfx/synchro_02");
            }
            if (!playedCircleA && elapsed >= 2.75f) {
                playedCircleA = true;
                SFXUtil.Play($"event:/vygo/sfx/synchro_03_0{circleVariant}");
            }
            if (!playedCircleB && elapsed >= 3.266667f) {
                playedCircleB = true;
                SFXUtil.Play($"event:/vygo/sfx/synchro_04_0{circleVariant}");
            }
            if (!playedPost && elapsed >= 3.683333f) {
                playedPost = true;
                SFXUtil.Play("event:/vygo/sfx/synchro_05");
            }
            UpdateMainStage(stage, elapsed);
        });
    }

    /// <summary>仅供演出测试使用；标准同调召唤 API 不会调用无素材分支。</summary>
    public async Task PlayNoMaterial(int targetLevel) {
        ClearStage();
        TimelineElapsed = 0f;
        Stage stage = BuildMainStage(4);
        SFXUtil.Play("event:/vygo/sfx/synchro_01_04");
        bool playedA = false;
        bool playedB = false;
        bool playedPost = false;
        await AnimateFor(NoMaterialDuration, (elapsed, _) => {
            TimelineElapsed = elapsed;
            // SummonSynchro02 相比主分支提前 1.083333 秒。
            float sourceElapsed = elapsed + 1.083333f;
            if (!playedA && elapsed >= 1.666667f) {
                playedA = true;
                SFXUtil.Play("event:/vygo/sfx/synchro_03_04");
            }
            if (!playedB && elapsed >= 2.183333f) {
                playedB = true;
                SFXUtil.Play("event:/vygo/sfx/synchro_04_04");
            }
            if (!playedPost && elapsed >= 2.6f) {
                playedPost = true;
                SFXUtil.Play("event:/vygo/sfx/synchro_05");
            }
            UpdateMainStage(stage, sourceElapsed);
        });
    }

    public async Task PlayForegroundPost() {
        ClearStage();
        FxMesh flare = CreateQuad(
            _flareTexture,
            new Vector2(22f, 22f),
            Colors.White,
            Vector3.Zero
        );
        List<FxMesh> rays = [];
        for (int i = 0; i < 16; i++) {
            FxMesh ray = CreateQuad(
                _flareTexture,
                new Vector2(0.46f + i % 3 * 0.12f, 14f),
                i % 3 == 0 ? Colors.White : TunerCyanBright,
                new Vector3(0f, 0f, -0.1f)
            );
            ray.Node.RotationDegrees = new Vector3(0f, 0f, i * 22.5f);
            rays.Add(ray);
        }

        await AnimateFor(MainDuration - PostStart, (elapsed, _) => {
            float strong = SmoothStep(Mathf.Clamp(
                (elapsed - (StrongSummon - PostStart)) / 0.18f,
                0f,
                1f
            ));
            float fade = 1f - SmoothStep(Mathf.Clamp((elapsed - 1.38f) / 0.36f, 0f, 1f));
            flare.Node.Scale = Vector3.One * Mathf.Lerp(0.08f, 1.65f, strong);
            SetAlpha(flare, Mathf.Sin(strong * Mathf.Pi) * 0.96f * fade);
            for (int i = 0; i < rays.Count; i++) {
                float rayT = SmoothStep(Mathf.Clamp(
                    (elapsed - (StrongSummon - PostStart) - 0.05f - i * 0.012f) / 0.3f,
                    0f,
                    1f
                ));
                rays[i].Node.Scale = new Vector3(1f, Mathf.Lerp(0.02f, 1f, rayT), 1f);
                SetAlpha(rays[i], Mathf.Sin(rayT * Mathf.Pi) * 0.68f * fade);
            }
        });
    }

    private Stage BuildMainStage(int circleVariant) {
        // Unity 的 SynchroCircleSet 是所有圆环的共同父节点：父节点只负责翻正和整体缩放，
        // 各层圆环在子节点上绕自身法线旋转。两类旋转不能写进同一个欧拉角。
        Node3D circlePlane = new() {
            Name = "SynchroCircleSet",
            Position = new Vector3(0f, 0f, -4f)
        };
        _stageRoot.AddChild(circlePlane);

        List<FxMesh> circles = [];
        int circleCount = circleVariant switch { 1 => 4, 2 => 6, 3 => 3, _ => 6 };
        // Circle02 prefab 六层网格经 Unity 的父节点比例折算到正交相机空间。
        float[] sourceScales = [0.25f, 0.25f, 0.25f, 0.25f, 0.161f, 0.183f];
        // Timeline 的 Circle05/Circle06 分别位于局部 y=+1.21/-0.96；父节点倾斜时
        // 会投影成上、中、下三层，翻正至 90° 后则转化为纯深度差并在屏幕上重合。
        float[] sourceOffsets = [0f, 0f, 0f, 0f, 1.21f, -0.96f];
        for (int i = 0; i < circleCount; i++) {
            FxMesh circle = CreateImportedSolidMesh(
                $"SynchroCircle{i + 1:00}.obj",
                TunerCyan,
                new Vector3(0f, sourceOffsets[i], 0f),
                circlePlane
            );
            circle.Node.Scale = Vector3.One * sourceScales[i];
            circle.BaseScale = circle.Node.Scale;
            circles.Add(circle);
        }
        FxMesh accent = CreateImportedSolidMesh(
            "SynchroCircleAccent.obj",
            TunerCyan,
            Vector3.Zero,
            circlePlane
        );
        accent.Node.Scale = Vector3.One * 0.265f;
        accent.BaseScale = accent.Node.Scale;
        circles.Add(accent);

        // 实机是围绕圆环的离散光点。固定公式确保联机双方轨迹一致。
        List<FxMesh> rays = [];
        for (int i = 0; i < 14; i++) {
            FxMesh ray = CreateQuad(
                _flareTexture,
                new Vector2(0.42f + i % 3 * 0.11f, 16f),
                i % 4 == 0 ? TunerCyanBright : TunerCyanDeep,
                new Vector3(0f, 0f, -4.35f)
            );
            ray.Node.RotationDegrees = new Vector3(0f, 0f, i * (180f / 14f));
            rays.Add(ray);
        }

        List<FxMesh> sparks = [];
        for (int i = 0; i < 52; i++) {
            float angle = i * Mathf.Tau / 52f;
            float radius = 2.0f + (i * 17 % 19) * 0.17f;
            float size = 0.32f + (i * 7 % 6) * 0.085f;
            FxMesh spark = CreateQuad(
                i % 5 == 0 ? _starTexture : _flareTexture,
                Vector2.One * size,
                i % 5 == 0 ? TunerCyanBright : TunerCyan,
                new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -1.6f)
            );
            sparks.Add(spark);
        }

        List<FxMesh> streaks = [];
        for (int i = 0; i < 22; i++) {
            float angle = i * Mathf.Tau / 22f;
            float radius = 3.1f + (i * 13 % 9) * 0.22f;
            FxMesh streak = CreateQuad(
                _flareTexture,
                new Vector2(0.2f + i % 3 * 0.07f, 2.6f + i % 4 * 0.42f),
                i % 4 == 0 ? TunerCyanBright : TunerCyan,
                new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -1.45f)
            );
            streak.Node.RotationDegrees = new Vector3(0f, 0f, -Mathf.RadToDeg(angle));
            streaks.Add(streak);
        }

        FxMesh coreGlow = CreateQuad(
            _flareTexture,
            new Vector2(18f, 18f),
            TunerCyanBright,
            new Vector3(0f, 0f, -3.55f)
        );

        // 核心白光使用两个独立加算层：大范围炫光负责持续照亮中心，
        // 小范围脉冲负责在翻正和结果卡出现时产生明显的白色爆发。
        FxMesh coreWhite = CreateQuad(
            _flareTexture,
            new Vector2(21f, 21f),
            Colors.White,
            new Vector3(0f, 0f, -3.42f)
        );
        FxMesh corePulse = CreateQuad(
            _flareTexture,
            new Vector2(15f, 15f),
            Colors.White,
            new Vector3(0f, 0f, -3.3f)
        );

        FxMesh centerFlare = CreateQuad(
            _flareTexture,
            new Vector2(12f, 12f),
            TunerCyanBright,
            new Vector3(0f, 0f, -1f)
        );
        return new Stage(
            circlePlane,
            circles,
            rays,
            sparks,
            streaks,
            coreGlow,
            coreWhite,
            corePulse,
            centerFlare
        );
    }

    private void UpdateMainStage(Stage stage, float elapsed) {
        float endFade = 1f - SmoothStep(Mathf.Clamp((elapsed - 4.7f) / 0.316667f, 0f, 1f));
        float ringT = SmoothStep(Mathf.Clamp((elapsed - RingStart) / 0.28f, 0f, 1f));
        // Unity Recorded (6)：父节点保持 75° 至 2.667 秒，随后在 3.333 秒翻正。
        // OBJ 原始面位于 XZ 平面，因此 Godot 相机空间需要额外加 90°基准旋转。
        float faceT = SmoothStep(Mathf.Clamp((elapsed - 2.666667f) / 0.666666f, 0f, 1f));
        stage.CirclePlane.RotationDegrees = new Vector3(
            Mathf.Lerp(165f, 90f, faceT),
            0f,
            0f
        );
        stage.CirclePlane.Scale = Vector3.One * SampleCircleSetScale(elapsed);
        float ringFade = 1f - SmoothStep(Mathf.Clamp((elapsed - 4.68f) / 0.336667f, 0f, 1f));
        float strong = SmoothStep(Mathf.Clamp((elapsed - StrongSummon) / 0.16f, 0f, 1f));
        float circleLocalTime = Mathf.Clamp(elapsed - RingStart, 0f, 3.666666f);
        for (int i = 0; i < stage.Circles.Count; i++) {
            FxMesh circle = stage.Circles[i];
            // 子环完整保留 Unity 的 X/Y/Z 曲线。X/Z 的 180°翻转完成时间和轴向各不相同，
            // 因而翻正途中呈现交错的 3D 角度；达到 180°后又会回到共同平面。
            circle.Node.RotationDegrees = SampleCircleRotation(
                i,
                stage.Circles.Count,
                circleLocalTime
            );
            float pulse = 0.96f + Mathf.Sin(elapsed * 5.2f + i * 0.8f) * 0.035f;
            circle.Node.Scale = circle.BaseScale * pulse;
            SetAlpha(circle, ringT * ringFade * (0.72f + i % 3 * 0.06f));
            circle.Material.SetShaderParameter("_FakeBlend", ringFade);
        }

        for (int i = 0; i < stage.Rays.Count; i++) {
            FxMesh ray = stage.Rays[i];
            ray.Node.RotationDegrees = new Vector3(
                0f,
                0f,
                i * (180f / stage.Rays.Count) + elapsed * (i % 2 == 0 ? 9f : -7f)
            );
            float rayPulse = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(elapsed * 3.7f + i));
            ray.Node.Scale = new Vector3(1f, Mathf.Lerp(0.12f, 1f, ringT), 1f);
            SetAlpha(ray, ringT * ringFade * rayPulse * 0.38f);
        }

        float sparkT = SmoothStep(Mathf.Clamp((elapsed - RingStart - 0.08f) / 0.2f, 0f, 1f));
        for (int i = 0; i < stage.Sparks.Count; i++) {
            FxMesh spark = stage.Sparks[i];
            float angle = i * Mathf.Tau / stage.Sparks.Count
                + elapsed * (i % 2 == 0 ? 0.8f : -0.55f);
            float radius = new Vector2(spark.BasePosition.X, spark.BasePosition.Y).Length()
                * Mathf.Lerp(1.12f, 0.62f, faceT);
            spark.Node.Position = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                spark.BasePosition.Z
            );
            float twinkle = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(elapsed * 8f + i * 1.73f));
            spark.Node.Scale = spark.BaseScale * Mathf.Lerp(0.2f, 1f, sparkT) * twinkle;
            SetAlpha(spark, sparkT * ringFade * (0.55f + twinkle * 0.45f));
        }

        float streakT = SmoothStep(Mathf.Clamp((elapsed - LevelStart) / 0.18f, 0f, 1f));
        for (int i = 0; i < stage.Streaks.Count; i++) {
            FxMesh streak = stage.Streaks[i];
            float cycle = Mathf.PosMod(elapsed * (0.78f + i % 5 * 0.08f) + i * 0.117f, 1f);
            float angle = i * Mathf.Tau / stage.Streaks.Count
                + elapsed * (i % 2 == 0 ? 0.42f : -0.34f);
            float radius = Mathf.Lerp(5.4f, 1.45f, cycle);
            streak.Node.Position = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                streak.BasePosition.Z
            );
            streak.Node.RotationDegrees = new Vector3(0f, 0f, -Mathf.RadToDeg(angle));
            float streakAlpha = Mathf.Sin(cycle * Mathf.Pi);
            SetAlpha(streak, streakT * ringFade * streakAlpha * 0.9f);
        }

        float post = SmoothStep(Mathf.Clamp((elapsed - PostStart) / 0.28f, 0f, 1f));
        float corePulse = 0.82f + 0.18f * Mathf.Sin(elapsed * 7.2f);
        stage.CoreGlow.Node.Scale = Vector3.One
            * Mathf.Lerp(0.22f, corePulse * 1.28f, ringT)
            * (1f + strong * 0.32f);
        SetAlpha(stage.CoreGlow, ringT * ringFade * (0.58f + faceT * 0.22f));

        float faceFlash = PulseAt(elapsed, 3.333333f, 0.42f);
        float summonFlash = PulseAt(elapsed, StrongSummon, 0.34f);
        float whiteBreath = 0.86f + 0.14f * Mathf.Sin(elapsed * 10.5f);
        stage.CoreWhite.Node.Scale = Vector3.One
            * (0.92f + whiteBreath * 0.34f + faceFlash * 0.42f + summonFlash * 0.62f);
        SetAlpha(
            stage.CoreWhite,
            ringT * ringFade * (0.62f + faceT * 0.16f + summonFlash * 0.22f)
        );
        stage.CorePulse.Node.Scale = Vector3.One
            * (0.28f + faceFlash * 1.18f + summonFlash * 1.72f);
        SetAlpha(
            stage.CorePulse,
            ringT * ringFade * Mathf.Clamp(faceFlash * 0.82f + summonFlash, 0f, 1f)
        );
        stage.CenterFlare.Node.Scale = Vector3.One * Mathf.Lerp(0.03f, 1.15f, strong);
        SetAlpha(stage.CenterFlare, post * Mathf.Sin(strong * Mathf.Pi) * 0.74f * endFade);
    }

    private static float PulseAt(float elapsed, float center, float halfWidth) {
        float distance = Mathf.Abs(elapsed - center) / halfWidth;
        return 1f - SmoothStep(Mathf.Clamp(distance, 0f, 1f));
    }

    private static float SampleCircleSetScale(float elapsed) {
        if (elapsed <= RingStart) return 0.6f;
        if (elapsed <= 1.833333f) {
            return Mathf.Lerp(0.6f, 1f, (elapsed - RingStart) / 0.5f);
        }
        if (elapsed <= StrongSummon) return 1f;
        return Mathf.Lerp(
            1f,
            3f,
            Mathf.Clamp((elapsed - StrongSummon) / (4.666667f - StrongSummon), 0f, 1f)
        );
    }

    private static Vector3 SampleCircleRotation(int index, int count, float time) {
        // Circle01..06 与 Accent 对应 Unity Recorded (13/11/14/10/8/9/12)。
        bool isAccent = index == count - 1;
        int sourceIndex = isAccent ? 6 : index;
        return sourceIndex switch {
            // Recorded (13)：Circle01，绕 Z 正向翻转。
            0 => new Vector3(
                0f,
                SampleRotationCurve(time, 1.333333f, 109.090675f, 2f, 163.636f, -360f),
                SampleHalfTurn(time, 1.333333f, 2f, 180f)
            ),
            // Recorded (11)：Circle02，绕 X 负向翻转。
            1 => new Vector3(
                SampleHalfTurn(time, 1.333333f, 2f, -180f),
                SampleRotationCurve(time, 1.333333f, -109.090675f, 2f, -163.636f, 360f),
                0f
            ),
            // Recorded (14)：Circle03，绕 Z 正向翻转。
            2 => new Vector3(
                0f,
                SampleRotationCurve(time, 1.333333f, 109.090675f, 2f, 163.636f, -360f),
                SampleHalfTurn(time, 1.333333f, 2f, 180f)
            ),
            // Recorded (10)：Circle04，绕 X 正向翻转。
            3 => new Vector3(
                SampleHalfTurn(time, 1.333333f, 2f, 180f),
                SampleRotationCurve(time, 1.333333f, -109.090675f, 2f, -163.636f, 360f),
                0f
            ),
            // Recorded (8)：Circle05 更早完成绕 Z 翻转。
            4 => new Vector3(
                0f,
                SampleRotationCurve(time, 1.333333f, -130.90881f, 1.666667f, -163.636f, -360f),
                SampleHalfTurn(time, 1.333333f, 1.666667f, 180f)
            ),
            // Recorded (9)：Circle06 更早完成绕 X 负向翻转。
            5 => new Vector3(
                SampleHalfTurn(time, 1.333333f, 1.666667f, -180f),
                SampleRotationCurve(time, 1.333333f, 65.45441f, 1.666667f, 81.818f, 180f),
                0f
            ),
            // Recorded (12)：Accent 绕 X 正向翻转。
            6 => new Vector3(
                SampleHalfTurn(time, 1.333333f, 2f, 180f),
                SampleRotationCurve(time, 1.333333f, -109.090675f, 2f, -163.636f, 360f),
                0f
            ),
            _ => Vector3.Zero
        };
    }

    private static float SampleHalfTurn(
        float time,
        float startTime,
        float endTime,
        float finalAngle
    ) {
        float progress = SmoothStep(Mathf.Clamp(
            (time - startTime) / (endTime - startTime),
            0f,
            1f
        ));
        return finalAngle * progress;
    }

    private static float SampleRotationCurve(
        float time,
        float firstTime,
        float firstValue,
        float secondTime,
        float secondValue,
        float finalValue
    ) {
        if (time <= firstTime) {
            return Mathf.Lerp(0f, firstValue, Mathf.Clamp(time / firstTime, 0f, 1f));
        }
        if (time <= secondTime) {
            return Mathf.Lerp(
                firstValue,
                secondValue,
                (time - firstTime) / (secondTime - firstTime)
            );
        }
        return Mathf.Lerp(
            secondValue,
            finalValue,
            Mathf.Clamp((time - secondTime) / (3.666666f - secondTime), 0f, 1f)
        );
    }

    private FxMesh CreateImportedSolidMesh(
        string filename,
        Color color,
        Vector3 position,
        Node3D? parent = null
    ) {
        Mesh? mesh = GD.Load<Mesh>($"res://VYgo/scenes/summon/synchro/assets/meshes/{filename}");
        if (mesh == null) {
            Entry.Logger.Error($"Failed to load converted Synchro mesh: {filename}");
            mesh = new QuadMesh { Size = new Vector2(4f, 4f) };
        }
        return CreateMesh(mesh, null, color, position, _solidShader, parent);
    }

    private FxMesh CreateQuad(Texture2D texture, Vector2 size, Color color, Vector3 position) {
        return CreateMesh(new QuadMesh { Size = size }, texture, color, position, _shader);
    }

    private FxMesh CreateMesh(
        Mesh mesh,
        Texture2D? texture,
        Color color,
        Vector3 position,
        Shader shader,
        Node3D? parent = null
    ) {
        ShaderMaterial material = new() { Shader = shader };
        material.SetShaderParameter("_TintColor", color);
        material.SetShaderParameter("_AddColor", Colors.Transparent);
        material.SetShaderParameter("_FakeBlend", 1f);
        if (texture != null) {
            material.SetShaderParameter("_MainTex", texture);
            material.SetShaderParameter("_Amplitude", 0f);
            material.SetShaderParameter("_offset", 0f);
            material.SetShaderParameter("_MainTex_ST", new Vector4(1f, 1f, 0f, 0f));
            material.SetShaderParameter("_RING_Radial", 1f);
        }
        MeshInstance3D node = new() {
            Mesh = mesh,
            MaterialOverride = material,
            Position = position,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        (parent ?? _stageRoot).AddChild(node);
        _materials.Add(material);
        return new FxMesh(node, material, color, position);
    }

    private async Task AnimateFor(float duration, Action<float, float> update) {
        float elapsed = 0f;
        update(0f, 0f);
        while (elapsed < duration && GodotObject.IsInstanceValid(this) && IsInsideTree()) {
            await this.AwaitProcessFrame();
            elapsed = Math.Min(duration, elapsed + (float)GetProcessDeltaTime());
            update(elapsed, SmoothStep(elapsed / duration));
        }
    }

    private void ClearStage() {
        if (_stageRoot == null) return;
        foreach (Node child in _stageRoot.GetChildren()) child.QueueFree();
        _materials.Clear();
    }

    private static void SetAlpha(FxMesh mesh, float alpha) {
        mesh.Material.SetShaderParameter(
            "_TintColor",
            new Color(mesh.BaseColor, Mathf.Clamp(alpha, 0f, 1f))
        );
    }

    private Texture2D LoadTexture(string filename) => GD.Load<Texture2D>(
        $"res://VYgo/scenes/summon/synchro/assets/textures/{filename}"
    );

    private static float SmoothStep(float value) {
        value = Mathf.Clamp(value, 0f, 1f);
        return value * value * (3f - 2f * value);
    }

    private sealed class FxMesh(
        MeshInstance3D node,
        ShaderMaterial material,
        Color baseColor,
        Vector3 basePosition
    ) {
        public MeshInstance3D Node { get; } = node;
        public ShaderMaterial Material { get; } = material;
        public Color BaseColor { get; } = baseColor;
        public Vector3 BasePosition { get; } = basePosition;
        public Vector3 BaseScale { get; set; } = Vector3.One;
    }

    private sealed record Stage(
        Node3D CirclePlane,
        List<FxMesh> Circles,
        List<FxMesh> Rays,
        List<FxMesh> Sparks,
        List<FxMesh> Streaks,
        FxMesh CoreGlow,
        FxMesh CoreWhite,
        FxMesh CorePulse,
        FxMesh CenterFlare
    );
}
