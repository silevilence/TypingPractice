using System.Text;
using TypingCore.Models;
using TypingCore.Parsing;

namespace TypingCore.Tests.Parsing;

public sealed class ArticleImportServiceTests : IDisposable
{
    private readonly string tempDirectory;
    private readonly ArticleImportService service;

    public ArticleImportServiceTests()
    {
        tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"TypingCore.ArticleImport.{Guid.NewGuid():N}");

        Directory.CreateDirectory(tempDirectory);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        service = new ArticleImportService();
    }

    [Fact]
    public void ImportFromText_normalizes_text_for_clipboard_flow()
    {
        ArticleImportResult result = service.ImportFromText(
            "剪贴板文章",
            "\r\n\r\n第一行\r\n\r\n\r\n第二行\r第三行\n\n");

        Assert.Equal("剪贴板文章", result.Title);
        Assert.Equal("第一行\n\n第二行\n第三行", result.NormalizedText);
        Assert.Null(result.DetectedEncodingName);

        Assert.Equal("第一行\n\n第二行\n第三行", result.Layout.NormalizedText);
    }

    [Fact]
    public async Task ImportFromFileAsync_detects_utf8_without_bom_and_uses_file_name_as_title()
    {
        string filePath = Path.Combine(tempDirectory, "mixed-text.txt");
        await File.WriteAllTextAsync(filePath, "中A\r\n\r\nB", new UTF8Encoding(false));

        ArticleImportResult result = await service.ImportFromFileAsync(filePath);

        Assert.Equal("mixed-text", result.Title);
        Assert.Equal("中A\n\nB", result.NormalizedText);
        Assert.StartsWith(
            "utf-8",
            result.DetectedEncodingName,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImportFromFileAsync_falls_back_to_gb18030_when_utf8_decode_fails()
    {
        string filePath = Path.Combine(tempDirectory, "gbk-sample.txt");
        byte[] bytes = Encoding.GetEncoding("GB18030").GetBytes("第一行\r\n\r\n\r\n第二行");
        await File.WriteAllBytesAsync(filePath, bytes);

        ArticleImportResult result = await service.ImportFromFileAsync(filePath);

        Assert.Equal("gbk-sample", result.Title);
        Assert.Equal("第一行\n\n第二行", result.NormalizedText);
        Assert.Equal("gb18030", result.DetectedEncodingName);
    }

    [Fact]
    public async Task ImportFromFileAsync_rejects_empty_file()
    {
        string filePath = Path.Combine(tempDirectory, "empty.txt");
        await File.WriteAllBytesAsync(filePath, Array.Empty<byte>());

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => service.ImportFromFileAsync(filePath));

        Assert.Contains("不能为空", exception.Message);
    }

    [Fact]
    public void ImportFromText_rejects_article_over_supported_length()
    {
        string text = new('中', Article.MaximumTextLength + 1);

        InvalidDataException exception = Assert.Throws<InvalidDataException>(
            () => service.ImportFromText("超长文章", text));

        Assert.Contains(Article.MaximumTextLength.ToString(), exception.Message);
    }

    [Fact]
    public void ImportFromText_throws_for_blank_title()
    {
        Assert.Throws<ArgumentException>(() => service.ImportFromText(" ", "正文"));
    }

    [Fact]
    public async Task ImportFromFileAsync_throws_for_blank_file_path()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => service.ImportFromFileAsync(" "));
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
