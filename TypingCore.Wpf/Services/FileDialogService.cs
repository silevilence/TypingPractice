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
}