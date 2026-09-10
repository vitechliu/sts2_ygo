using Godot;

namespace VYgo.Core.Effects;

/// <summary>独立视觉审核入口，所有状态均为演示，不访问战斗或怪兽模型。</summary>
[ScriptPath("res://Core/Effects/NMonsterAuraDemo.cs")]
public partial class NMonsterAuraDemo : Control {
    private readonly List<NMonsterAura> _auras = [];
    private Label _stageLabel = null!;
    private Label _description = null!;
    private Button _autoButton = null!;
    private ProgressBar _progress = null!;
    private bool _auto = true;
    private double _elapsed;
    private int _stage = -1;
    private bool _demoHover;
    private static readonly string[] Titles = ["淡灰常态", "召唤 · 中心放大", "淡绿 · 可行动", "行动成功 · 渐暗", "恢复 · 平滑亮起", "死亡 · 淡出收缩"];
    private static readonly string[] Descriptions = [
        "光环保持非常淡的灰色底光，刻线持续缓慢旋转。",
        "召唤时光环从中心平滑放大，并逐渐显现为可行动的淡绿色。",
        "只要当前仍可行动，就持续保持亮色；多次行动仍有机会时亦如此。",
        "模拟成功执行最后一次行动：短暂反馈后，颜色与亮度缓慢退至淡灰。",
        "模拟行动恢复：从淡灰逐渐转亮为淡绿，旋转速度平滑变化。",
        "模拟怪兽死亡：光环先原地变淡，再向中心缩小并完全消失。"
    ];

    public override void _Ready() {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var font = new SystemFont { FontNames = ["Microsoft YaHei UI", "Microsoft YaHei"] };
        Theme = new Theme { DefaultFont = font, DefaultFontSize = 18 };
        var background = new ColorRect {
            Color = new Color("171c1b"), MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(background);
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddLabel("怪兽光环 / 视觉预览", new Vector2(48, 30), 28, new Color("e6eee8"));
        AddLabel("独立 Godot 场景 · 模拟行动状态 · 与战斗怪兽共用光环资源", new Vector2(48, 76), 16, new Color("8eaaa0"));

        float[] widths = [128f, 192f, 288f];
        float[] centers = [220f, 530f, 870f];
        string[] sizes = ["小", "中", "大"];
        PackedScene scene = ResourceLoader.Load<PackedScene>(NMonsterAura.ScenePath);
        for (int i = 0; i < widths.Length; i++) {
            NMonsterAura aura = scene.Instantiate<NMonsterAura>();
            aura.AuraWidth = widths[i];
            aura.Position = new Vector2(centers[i], 325f);
            AddChild(aura);
            _auras.Add(aura);
            AddLabel($"{sizes[i]}  /  宽 {widths[i]:0}", new Vector2(centers[i] - 64, 418), 18, new Color("b4c7bd"));
        }
        AddLabel("固定椭圆比例 4 : 1 · 尺寸只做整体缩放", new Vector2(48, 468), 16, new Color("8eaaa0"));
        _stageLabel = AddLabel("", new Vector2(48, 130), 24, new Color("c7ebd1"));
        _description = AddLabel("", new Vector2(48, 175), 17, new Color("a9bdb2"));
        _progress = new ProgressBar {
            Position = new Vector2(48, 224), Size = new Vector2(1024, 4),
            MaxValue = 5, ShowPercentage = false, MouseFilter = MouseFilterEnum.Ignore
        };
        _progress.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = new Color("293b32") });
        _progress.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = new Color("72977f") });
        AddChild(_progress);
        for (int i = 0; i < Titles.Length; i++) {
            int selectedStage = i;
            var button = new Button {
                Text = Titles[i], Position = new Vector2(48 + i * 173, 523), Size = new Vector2(164, 46)
            };
            button.AddThemeFontSizeOverride("font_size", 16);
            button.Pressed += () => { _auto = false; _autoButton.Text = "自动演示：关"; SetStage(selectedStage); };
            AddChild(button);
        }
        _autoButton = new Button { Text = "自动演示：开", Position = new Vector2(48, 593), Size = new Vector2(170, 40) };
        _autoButton.Pressed += () => {
            _auto = !_auto;
            _autoButton.Text = _auto ? "自动演示：开" : "自动演示：关";
            _elapsed = 0;
        };
        AddChild(_autoButton);
        AddLabel("亮色", new Vector2(248, 600), 16, new Color("a9bdb2"));
        var colorPicker = new ColorPickerButton {
            Color = _auras[0].ActiveColor, Position = new Vector2(297, 593), Size = new Vector2(82, 40), EditAlpha = false
        };
        colorPicker.ColorChanged += color => { foreach (NMonsterAura aura in _auras) aura.ActiveColor = color; };
        AddChild(colorPicker);
        AddLabel("过渡", new Vector2(409, 600), 16, new Color("a9bdb2"));
        var duration = new HSlider {
            MinValue = 0.5, MaxValue = 3, Step = 0.1, Value = 1.2,
            Position = new Vector2(462, 602), Size = new Vector2(168, 24)
        };
        Label durationLabel = AddLabel("1.2 秒", new Vector2(648, 600), 16, new Color("a9bdb2"));
        duration.ValueChanged += value => {
            foreach (NMonsterAura aura in _auras) aura.TransitionSeconds = (float)value;
            durationLabel.Text = $"{value:0.0} 秒";
        };
        AddChild(duration);
        var hover = new CheckButton { Text = "悬停律动", Position = new Vector2(784, 593), Size = new Vector2(200, 40) };
        hover.Toggled += enabled => {
            _demoHover = enabled;
            foreach (NMonsterAura aura in _auras) aura.SetHovered(enabled);
        };
        AddChild(hover);
        AddLabel("每阶段 5 秒，自动循环；也可点击切换", new Vector2(48, 657), 14, new Color("789186"));
        SetStage(0);
    }

    public override void _Process(double delta) {
        if (!_auto) return;
        _elapsed += delta;
        if (_elapsed >= 5) SetStage((_stage + 1) % Titles.Length);
        _progress.Value = _elapsed;
    }

    private void SetStage(int stage) {
        _stage = stage;
        _elapsed = 0;
        _progress.Value = 0;
        _stageLabel.Text = $"0{stage + 1}  /  {Titles[stage]}";
        _description.Text = Descriptions[stage];
        foreach (NMonsterAura aura in _auras) {
            aura.ResetPresentation();
            aura.SetHovered(_demoHover);
            aura.SetState(stage is 1 or 2 or 4 or 5 ? MonsterAuraState.Available : MonsterAuraState.NoAction);
            if (stage == 1) aura.PlaySummonFeedback();
            if (stage == 3) aura.PlayActionFeedback();
            if (stage == 5) aura.PlayDeathFeedback();
        }
    }

    private Label AddLabel(string text, Vector2 position, int fontSize, Color color) {
        var label = new Label { Text = text, Position = position, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        AddChild(label);
        return label;
    }
}
