namespace TypingCore.Wpf.ViewModels;

/// <summary>
/// Provides the stage-seven settings page scaffold.
/// </summary>
public sealed class SettingsViewModel : PageViewModel
{
    public SettingsViewModel()
        : base("设置")
    {
        ThemeOptions = new[] { "跟随系统", "晨纸浅色", "夜色深色" };
        FontOptions = new[] { "霞鹜文楷", "等距更纱黑体 SC", "微软雅黑" };
        ShortcutHints = new[]
        {
            "暂停 / 继续：后续阶段会支持自定义快捷键。",
            "重来一篇：保留占位入口，等输入流程接入后启用。",
            "切换布局：阶段十会在这里接入 A/B 布局切换。",
        };

        SelectedTheme = ThemeOptions[0];
        SelectedFont = FontOptions[0];
        Description = "当前页面先承接主题、字体和快捷键配置的结构，后续阶段会补充持久化和即时生效逻辑。";
    }

    public IReadOnlyList<string> ThemeOptions { get; }

    public IReadOnlyList<string> FontOptions { get; }

    public IReadOnlyList<string> ShortcutHints { get; }

    public string SelectedTheme { get; }

    public string SelectedFont { get; }

    public string Description { get; }
}