using System.Globalization;
using TypingCore.Abstractions;
using TypingCore.Models;

namespace TypingCore.Parsing;

/// <summary>
/// Parses tab-separated code tables in code, word, and optional weight order.
/// </summary>
/// <remarks>
/// Instances are stateless and safe for concurrent use. Optional YAML headers are ignored.
/// </remarks>
public sealed class CodeTableParser : ICodeTableParser
{
    /// <inheritdoc />
    public CodeTable Parse(
        string name,
        string source,
        string content,
        DateTimeOffset loadedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentNullException.ThrowIfNull(content);

        string[] lines = content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
        int firstContentLine = Array.FindIndex(
            lines,
            line => !string.IsNullOrWhiteSpace(line));
        bool isInYamlHeader = firstContentLine >= 0
            && string.Equals(lines[firstContentLine].Trim(), "---", StringComparison.Ordinal);
        List<CodeTableEntry> entries = [];

        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index].Trim();

            if (isInYamlHeader)
            {
                if (index > firstContentLine
                    && string.Equals(line, "---", StringComparison.Ordinal))
                {
                    isInYamlHeader = false;
                }

                continue;
            }

            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            string[] columns = line.Contains('\t')
                ? line.Split('\t')
                : line.Split("<TAB>", StringSplitOptions.None);

            if (columns.Length < 2
                || string.IsNullOrWhiteSpace(columns[0])
                || string.IsNullOrWhiteSpace(columns[1]))
            {
                throw new FormatException($"码表第 {index + 1} 行格式无效，应为 code<TAB>word[<TAB>weight]。");
            }

            int weight = 0;
            if (columns.Length >= 3
                && !string.IsNullOrWhiteSpace(columns[2])
                && !int.TryParse(
                    columns[2].Trim(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out weight))
            {
                throw new FormatException($"码表第 {index + 1} 行权重无效。");
            }

            entries.Add(new CodeTableEntry(
                columns[0].Trim(),
                new[] { columns[1].Trim() },
                weight));
        }

        if (isInYamlHeader)
        {
            throw new FormatException("码表 YAML 头缺少结束标记 ---。");
        }

        if (entries.Count == 0)
        {
            throw new FormatException("码表中没有可用条目。");
        }

        return new CodeTable(name, source, loadedAt, entries);
    }

    /// <inheritdoc />
    public async Task<CodeTable> ImportFromFileAsync(
        string filePath,
        DateTimeOffset loadedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        byte[] bytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
        (string content, _) = TextFileDecoder.Decode(bytes);
        return Parse(
            Path.GetFileNameWithoutExtension(filePath),
            filePath,
            content,
            loadedAt);
    }
}
