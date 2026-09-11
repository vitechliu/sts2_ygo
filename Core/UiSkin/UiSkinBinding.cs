using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace VYgo.Core.UiSkin;

/// <summary>一个图层的状态、实例资源和可选布局调整。</summary>
internal sealed class UiSkinBinding(Node node, Node sceneRoot, UiSkinRule rule, Texture2D? original) {
    public Node Node => node;
    public UiSkinRule Rule => rule;
    public Control? Owner { get; private set; }
    private readonly Dictionary<string, Texture2D> _textures = [];
    private UiSkinVisual? _visual;
    private StyleBoxTexture? _style;
    private bool _hovered;
    private bool _pressed;
    private readonly List<Action> _disconnect = [];

    public void Install() {
        foreach (var (state, path) in rule.States) _textures[state] = ResourceLoader.Load<Texture2D>(path) ?? throw new InvalidDataException($"图片不可读：{path}");
        var size = _textures["normal"].GetSize();
        if (_textures.Values.Any(t => t.GetSize() != size)) throw new InvalidDataException("状态图尺寸不一致");
        if (rule.Mode == "nine" && (rule.Border[0] + rule.Border[2] >= size.X || rule.Border[1] + rule.Border[3] >= size.Y))
            throw new InvalidDataException("九宫格中间区域为空");
        for (Node? parent = node; parent != null; parent = parent.GetParent()) {
            if (parent is NClickableControl or BaseButton or NScrollbar) { Owner = (Control)parent; break; }
        }
        Owner ??= node as Control;
        // 在变更视觉前预检所有布局节点，保证错误规则不会留下半套布局。
        ValidateLayout();
        if (rule.Property.StartsWith("theme_override_styles/") && node is Control themed) {
            _style = (StyleBoxTexture)themed.GetThemeStylebox(rule.Property.Split('/')[1]).Duplicate();
            if (rule.Mode == "nine") for (int i = 0; i < 4; i++) _style.SetTextureMargin((Side)i, rule.Border[i]);
            if (rule.Mode == "nine") _style.RegionRect = new Rect2();
            if (rule.Mode == "tile") {
                _style.RegionRect = new Rect2();
                for (int i = 0; i < 4; i++) _style.SetTextureMargin((Side)i, 0);
                _style.AxisStretchHorizontal = StyleBoxTexture.AxisStretchMode.Tile; _style.AxisStretchVertical = StyleBoxTexture.AxisStretchMode.Tile;
            }
            themed.AddThemeStyleboxOverride(rule.Property.Split('/')[1], _style);
        } else if (node is Control host && (rule.Neutralize || rule.Mode is "nine" or "tile" || node is ColorRect) && node is TextureRect or NinePatchRect or ColorRect) {
            // 独立绘制层保留原节点及输入，并按规则隔离原版持续写入的染色与 Shader。
            _visual = new UiSkinVisual { Name = "VYgoSkinVisual" };
            _visual.Configure(host, rule);
            host.AddChild(_visual);
        }
        if (node is NinePatchRect nine && _visual == null && rule.Mode != "nine") {
            // 普通适配输出保留原逻辑画布，因此不改原九宫格和阴影采样偏移。
            nine.Texture = _textures["normal"];
        }
        if (Owner != null) {
            Action entered = () => { _hovered = true; Refresh(); };
            Action exited = () => { _hovered = false; _pressed = false; Refresh(); };
            Action visibility = () => { if (!Owner.IsVisibleInTree()) _pressed = false; Refresh(); };
            Control.GuiInputEventHandler input = e => { if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left } mb) { _pressed = mb.Pressed; Refresh(); } };
            Owner.MouseEntered += entered;
            Owner.MouseExited += exited;
            Owner.FocusEntered += Refresh;
            Owner.FocusExited += Refresh;
            Owner.VisibilityChanged += visibility;
            Owner.GuiInput += input;
            _disconnect.Add(() => { Owner.MouseEntered -= entered; Owner.MouseExited -= exited; Owner.FocusEntered -= Refresh; Owner.FocusExited -= Refresh; Owner.VisibilityChanged -= visibility; Owner.GuiInput -= input; });
            if (Owner is BaseButton button) {
                BaseButton.ToggledEventHandler toggled = _ => Refresh();
                Action down = () => { _pressed = true; Refresh(); }, up = () => { _pressed = false; Refresh(); };
                button.Toggled += toggled; button.ButtonDown += down; button.ButtonUp += up;
                _disconnect.Add(() => { button.Toggled -= toggled; button.ButtonDown -= down; button.ButtonUp -= up; });
            }
            node.TreeExiting += () => { if (GodotObject.IsInstanceValid(Owner)) foreach (var disconnect in _disconnect) disconnect(); _disconnect.Clear(); };
        }
        ApplyLayout();
        Refresh();
    }
    private void ValidateLayout() {
        foreach (var edit in new[] { rule.Layout.Visual, rule.Layout.Hitbox }) if (edit != null) {
            if (rule.Priority != 1 || !UiSkinService.IsRelativePath(edit.NodePath) || edit.Offsets.Length != 4 || sceneRoot.GetNodeOrNull<Control>(edit.NodePath) == null)
                throw new InvalidDataException("局部布局目标无效");
        }
        if (rule.Layout.Text is { } text && (rule.Priority != 1 || !UiSkinService.IsRelativePath(text.NodePath) || text.Margins.Length != 4 || sceneRoot.GetNodeOrNull<MarginContainer>(text.NodePath) == null))
            throw new InvalidDataException("文字区域必须指定 MarginContainer");
    }
    private void ApplyLayout() {
        foreach (var edit in new[] { rule.Layout.Visual, rule.Layout.Hitbox }) if (edit != null) {
            var control = sceneRoot.GetNode<Control>(edit.NodePath);
            for (int i = 0; i < 4; i++) control.SetOffset((Side)i, edit.Offsets[i]);
        }
        if (rule.Layout.Text is { } text) {
            var control = sceneRoot.GetNode<MarginContainer>(text.NodePath);
            string[] sides = ["left", "top", "right", "bottom"];
            for (int i = 0; i < 4; i++) control.AddThemeConstantOverride("margin_" + sides[i], text.Margins[i]);
        }
    }
    public void Refresh() {
        if (!GodotObject.IsInstanceValid(node) || !node.IsInsideTree()) return;
        try {
            bool selected = Owner is NTickbox tick && tick.IsTicked || Owner is NSettingsTab tab && ReadBool(tab, "_isSelected") || Owner is BaseButton { ToggleMode: true, ButtonPressed: true };
            selected |= Owner is NCardPoolFilter { IsSelected: true } or NCardTypeTickbox { IsTicked: true } or NCardCostTickbox { IsTicked: true } or NCardViewSortButton { IsDescending: true };
            bool disabled = Owner is NClickableControl { IsEnabled: false } || Owner is BaseButton { Disabled: true };
            bool focused = _hovered || Owner?.HasFocus() == true || Owner is NClickableControl clickable && AccessTools.Property(typeof(NClickableControl), "IsFocused")?.GetValue(clickable) is true;
            bool pressed = Owner is NClickableControl c ? ReadBool(c, "_isPressed") && focused : Owner is NScrollbar s ? ReadBool(s, "_isDragging") : _pressed;
            if (Owner is NDropdownScrollbar dropdown) pressed = dropdown.hasControl;
            string state = disabled ? "disabled" : pressed ? "pressed" : focused ? "hover" : "normal";
            string combined = selected ? state == "normal" ? "selected" : "selected_" + state : state;
            Texture2D texture = _textures.GetValueOrDefault(combined) ?? (selected ? _textures.GetValueOrDefault("selected") : null) ?? _textures.GetValueOrDefault(state) ?? _textures["normal"];
            if (_visual != null) _visual.SetTexture(texture);
            else if (_style != null) _style.Texture = texture;
            else node.Set(rule.Property, texture);
        } catch (Exception e) {
            UiSkinService.Warn(rule.Id, $"状态更新失败，恢复原图：{e.Message}");
            if (_style != null) _style.Texture = original;
            else if (_visual != null) _visual.Restore();
            else node.Set(rule.Property, original);
        }
    }
    private static bool ReadBool(object instance, string field) => AccessTools.Field(instance.GetType(), field)?.GetValue(instance) is true;
}
