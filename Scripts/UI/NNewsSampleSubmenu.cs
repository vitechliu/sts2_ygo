using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using VYgo.Core.News;

namespace VYgo.Scripts.UI;

/// <summary>公告样例：复用原版更新日志正文与滚动结构，接入原生子菜单返回流程。</summary>
public partial class NNewsSampleSubmenu : NSubmenu {
    protected override Control? InitialFocusedControl => GetNode<Control>("BackButton");

    public override void _Ready() {
        ConnectSignals();
        RefreshTexts();
        ProcessMode = ProcessModeEnum.Disabled;
    }

    protected override void OnSubmenuShown() {
        ProcessMode = ProcessModeEnum.Inherit;
        RefreshTexts();
        GetNode<NScrollableContainer>("ScreenContents").InstantlyScrollToTop();
    }

    protected override void OnSubmenuHidden() => ProcessMode = ProcessModeEnum.Disabled;

    public override void _Notification(int what) {
        if (what == NotificationTranslationChanged && IsNodeReady()) RefreshTexts();
    }

    private void RefreshTexts() {
        var texts = NewsLocalData.LoadPageTexts();
        GetNode<MegaLabel>("ScreenContents/Content/PatchText/DateLabel")
            .SetTextAutoSize(texts.GetValueOrDefault("NEWS_SAMPLE_TITLE", "VYgo"));
        GetNode<MegaRichTextLabel>("ScreenContents/Content/PatchText")
            .SetTextAutoSize(texts.GetValueOrDefault("NEWS_SAMPLE_BODY", ""));
    }
}
