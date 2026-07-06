using System.Text;

namespace TypingCore.Parsing;

internal static class TextFileDecoder
{
    private static readonly UTF8Encoding StrictUtf8Encoding = new(false, true);

    public static (string Text, string EncodingName) Decode(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        if (bytes.Length == 0)
        {
            return (string.Empty, StrictUtf8Encoding.WebName);
        }

        (Encoding? encoding, int bomLength) = DetectBom(bytes);
        if (encoding is not null)
        {
            return (
                encoding.GetString(bytes, bomLength, bytes.Length - bomLength),
                encoding.WebName);
        }

        try
        {
            return (StrictUtf8Encoding.GetString(bytes), StrictUtf8Encoding.WebName);
        }
        catch (DecoderFallbackException)
        {
            try
            {
                Encoding gb18030 = Encoding.GetEncoding("GB18030");
                return (gb18030.GetString(bytes), gb18030.WebName);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException(
                    "GB18030 fallback decoding is unavailable on this platform.",
                    ex);
            }
        }
    }

    private static (Encoding? Encoding, int BomLength) DetectBom(byte[] bytes)
    {
        if (HasPrefix(bytes, 0xEF, 0xBB, 0xBF))
        {
            return (new UTF8Encoding(true, true), 3);
        }

        if (HasPrefix(bytes, 0xFF, 0xFE, 0x00, 0x00))
        {
            return (new UTF32Encoding(false, true, true), 4);
        }

        if (HasPrefix(bytes, 0x00, 0x00, 0xFE, 0xFF))
        {
            return (new UTF32Encoding(true, true, true), 4);
        }

        if (HasPrefix(bytes, 0xFF, 0xFE))
        {
            return (new UnicodeEncoding(false, true, true), 2);
        }

        if (HasPrefix(bytes, 0xFE, 0xFF))
        {
            return (new UnicodeEncoding(true, true, true), 2);
        }

        return (null, 0);
    }

    private static bool HasPrefix(byte[] bytes, params byte[] prefix)
    {
        if (bytes.Length < prefix.Length)
        {
            return false;
        }

        for (int index = 0; index < prefix.Length; index++)
        {
            if (bytes[index] != prefix[index])
            {
                return false;
            }
        }

        return true;
    }
}
