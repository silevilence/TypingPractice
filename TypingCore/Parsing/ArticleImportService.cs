using TypingCore.Abstractions;
using TypingCore.Models;

namespace TypingCore.Parsing;

/// <summary>
/// Imports article text from caller-provided text or text files.
/// </summary>
/// <remarks>
/// Instances are safe for concurrent use when the supplied layout builder is safe for concurrent use.
/// </remarks>
public sealed class ArticleImportService : IArticleImportService
{
    private readonly IArticleTextLayoutBuilder layoutBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArticleImportService"/> class.
    /// </summary>
    public ArticleImportService()
        : this(new ArticleTextLayoutBuilder())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArticleImportService"/> class.
    /// </summary>
    /// <param name="layoutBuilder">The layout builder used to normalize imported text.</param>
    public ArticleImportService(IArticleTextLayoutBuilder layoutBuilder)
    {
        ArgumentNullException.ThrowIfNull(layoutBuilder);

        this.layoutBuilder = layoutBuilder;
    }

    /// <inheritdoc />
    public ArticleImportResult ImportFromText(string title, string rawText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(rawText);

        ArticleTextLayout layout = BuildValidatedLayout(rawText);
        return new ArticleImportResult(title, layout, detectedEncodingName: null);
    }

    /// <inheritdoc />
    public async Task<ArticleImportResult> ImportFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        long maximumFileBytes = ((long)Article.MaximumTextLength * 4) + 4;
        if (new FileInfo(filePath).Length > maximumFileBytes)
        {
            throw new InvalidDataException(
                $"文章内容不能超过 {Article.MaximumTextLength} 个字符。");
        }

        byte[] fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
        (string rawText, string encodingName) = TextFileDecoder.Decode(fileBytes);

        ArticleTextLayout layout = BuildValidatedLayout(rawText);
        string title = Path.GetFileNameWithoutExtension(filePath);

        return new ArticleImportResult(title, layout, encodingName);
    }

    private ArticleTextLayout BuildValidatedLayout(string rawText)
    {
        ArticleTextLayout layout = layoutBuilder.Build(rawText);
        if (layout.NormalizedText.Length == 0)
        {
            throw new InvalidDataException("文章内容不能为空。");
        }

        if (layout.NormalizedText.Length > Article.MaximumTextLength)
        {
            throw new InvalidDataException(
                $"文章内容不能超过 {Article.MaximumTextLength} 个字符。");
        }

        return layout;
    }
}
