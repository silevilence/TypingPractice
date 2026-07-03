using System.Text;
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
    private static readonly UTF8Encoding StrictUtf8Encoding = new(false, true);
    private readonly IArticleTextLayoutBuilder layoutBuilder;
    private readonly Encoding? gb18030Encoding;

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
        gb18030Encoding = TryGetGb18030Encoding();
    }

    /// <inheritdoc />
    public ArticleImportResult ImportFromText(string title, string rawText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(rawText);

        ArticleTextLayout layout = layoutBuilder.Build(rawText);
        return new ArticleImportResult(title, layout, detectedEncodingName: null);
    }

    /// <inheritdoc />
    public async Task<ArticleImportResult> ImportFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        byte[] fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
        (string rawText, string encodingName) = DecodeText(fileBytes);

        ArticleTextLayout layout = layoutBuilder.Build(rawText);
        string title = Path.GetFileNameWithoutExtension(filePath);

        return new ArticleImportResult(title, layout, encodingName);
    }

    private (string RawText, string EncodingName) DecodeText(byte[] fileBytes)
    {
        if (fileBytes.Length == 0)
        {
            return (string.Empty, StrictUtf8Encoding.WebName);
        }

        if (TryDecodeWithBom(fileBytes, out string? rawText, out string? encodingName))
        {
            return (rawText!, encodingName!);
        }

        try
        {
            return (StrictUtf8Encoding.GetString(fileBytes), StrictUtf8Encoding.WebName);
        }
        catch (DecoderFallbackException) when (gb18030Encoding is not null)
        {
            return (gb18030Encoding.GetString(fileBytes), gb18030Encoding.WebName);
        }
        catch (DecoderFallbackException ex)
        {
            throw new InvalidOperationException(
                "GB18030 fallback decoding is unavailable on this platform.",
                ex);
        }
    }

    private static Encoding? TryGetGb18030Encoding()
    {
        try
        {
            return Encoding.GetEncoding("GB18030");
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static bool TryDecodeWithBom(
        byte[] fileBytes,
        out string? rawText,
        out string? encodingName)
    {
        (Encoding? encoding, int bomLength) = DetectBom(fileBytes);

        if (encoding is null)
        {
            rawText = null;
            encodingName = null;
            return false;
        }

        rawText = encoding.GetString(fileBytes, bomLength, fileBytes.Length - bomLength);
        encodingName = encoding.WebName;
        return true;
    }

    private static (Encoding? Encoding, int BomLength) DetectBom(byte[] fileBytes)
    {
        if (HasPrefix(fileBytes, 0xEF, 0xBB, 0xBF))
        {
            return (new UTF8Encoding(true, true), 3);
        }

        if (HasPrefix(fileBytes, 0xFF, 0xFE, 0x00, 0x00))
        {
            return (new UTF32Encoding(false, true, true), 4);
        }

        if (HasPrefix(fileBytes, 0x00, 0x00, 0xFE, 0xFF))
        {
            return (new UTF32Encoding(true, true, true), 4);
        }

        if (HasPrefix(fileBytes, 0xFF, 0xFE))
        {
            return (new UnicodeEncoding(false, true, true), 2);
        }

        if (HasPrefix(fileBytes, 0xFE, 0xFF))
        {
            return (new UnicodeEncoding(true, true, true), 2);
        }

        return (null, 0);
    }

    private static bool HasPrefix(byte[] fileBytes, params byte[] prefix)
    {
        if (fileBytes.Length < prefix.Length)
        {
            return false;
        }

        for (int index = 0; index < prefix.Length; index++)
        {
            if (fileBytes[index] != prefix[index])
            {
                return false;
            }
        }

        return true;
    }
}