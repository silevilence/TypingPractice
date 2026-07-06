using Microsoft.Win32;

namespace TypingCore.Wpf.Services;

internal sealed class FileDialogService : IFileDialogService
{
    public string? SelectArticleFile()
    {
        OpenFileDialog dialog = new()
        {
            Title = "选择文章文件",
            Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SelectCodeTableFile()
    {
        OpenFileDialog dialog = new()
        {
            Title = "选择码表文件",
            Filter = "码表文件 (*.dict)|*.dict|文本码表 (*.txt;*.mb;*.yaml;*.yml)|*.txt;*.mb;*.yaml;*.yml|所有文件 (*.*)|*.*",
            DefaultExt = ".dict",
            FilterIndex = 1,
            CheckFileExists = true,
            Multiselect = false,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
