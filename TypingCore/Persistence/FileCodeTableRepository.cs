using TypingCore.Abstractions;
using TypingCore.Models;

namespace TypingCore.Persistence;

/// <summary>
/// Persists code tables as copied files inside an application-owned directory.
/// </summary>
/// <remarks>
/// Instances are intended for serialized application-level access and are not thread-safe.
/// </remarks>
public sealed class FileCodeTableRepository : ICodeTableRepository
{
    private const string ActiveFileName = ".active-code-table";
    private readonly string activeFilePath;
    private readonly ICodeTableParser parser;
    private readonly string storageDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileCodeTableRepository"/> class.
    /// </summary>
    /// <param name="storageDirectory">The application-owned code-table directory.</param>
    /// <param name="parser">The parser used to validate and load stored files.</param>
    public FileCodeTableRepository(string storageDirectory, ICodeTableParser parser)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageDirectory);
        ArgumentNullException.ThrowIfNull(parser);

        this.storageDirectory = Path.GetFullPath(storageDirectory);
        this.parser = parser;
        activeFilePath = Path.Combine(this.storageDirectory, ActiveFileName);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CodeTable>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(storageDirectory);
        List<CodeTable> tables = [];

        foreach (string filePath in Directory
                     .EnumerateFiles(storageDirectory)
                     .Where(path => !string.Equals(path, activeFilePath, StringComparison.OrdinalIgnoreCase))
                     .Where(path => !path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            DateTimeOffset loadedAt = new(File.GetLastWriteTimeUtc(filePath));
            tables.Add(await parser
                .ImportFromFileAsync(filePath, loadedAt, cancellationToken)
                .ConfigureAwait(false));
        }

        return tables;
    }

    /// <inheritdoc />
    public async Task<CodeTable> ImportAsync(
        string sourceFilePath,
        DateTimeOffset loadedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilePath);

        Directory.CreateDirectory(storageDirectory);
        string sourcePath = Path.GetFullPath(sourceFilePath);
        string targetPath = Path.Combine(storageDirectory, Path.GetFileName(sourcePath));

        // ponytail: filenames are table identity; add generated IDs only if same-name tables must coexist.
        if (string.Equals(sourcePath, targetPath, StringComparison.OrdinalIgnoreCase))
        {
            return await parser
                .ImportFromFileAsync(targetPath, loadedAt, cancellationToken)
                .ConfigureAwait(false);
        }

        string tempPath = $"{targetPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (FileStream source = File.OpenRead(sourcePath))
            await using (FileStream target = new(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                await source.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
            }

            CodeTable parsed = await parser
                .ImportFromFileAsync(tempPath, loadedAt, cancellationToken)
                .ConfigureAwait(false);
            File.Move(tempPath, targetPath, overwrite: true);
            File.SetLastWriteTimeUtc(targetPath, loadedAt.UtcDateTime);

            return new CodeTable(
                Path.GetFileNameWithoutExtension(targetPath),
                targetPath,
                loadedAt,
                parsed.Entries);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        string storedSource,
        CancellationToken cancellationToken = default)
    {
        string storedPath = GetStoredPath(storedSource);
        cancellationToken.ThrowIfCancellationRequested();

        if (File.Exists(storedPath))
        {
            File.Delete(storedPath);
        }

        string? activeSource = await GetActiveSourceAsync(cancellationToken).ConfigureAwait(false);
        if (string.Equals(activeSource, storedPath, StringComparison.OrdinalIgnoreCase))
        {
            await SetActiveSourceAsync(null, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetActiveSourceAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(activeFilePath))
        {
            return null;
        }

        string fileName = (await File
                .ReadAllTextAsync(activeFilePath, cancellationToken)
                .ConfigureAwait(false))
            .Trim();
        if (fileName.Length == 0
            || !string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal))
        {
            return null;
        }

        string storedPath = Path.Combine(storageDirectory, fileName);
        return File.Exists(storedPath) ? storedPath : null;
    }

    /// <inheritdoc />
    public async Task SetActiveSourceAsync(
        string? storedSource,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(storageDirectory);

        if (storedSource is null)
        {
            if (File.Exists(activeFilePath))
            {
                File.Delete(activeFilePath);
            }

            return;
        }

        string storedPath = GetStoredPath(storedSource);
        if (!File.Exists(storedPath))
        {
            throw new FileNotFoundException("无法启用不存在的码表文件。", storedPath);
        }

        await File.WriteAllTextAsync(
            activeFilePath,
            Path.GetFileName(storedPath),
            cancellationToken).ConfigureAwait(false);
    }

    private string GetStoredPath(string storedSource)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storedSource);

        string suppliedPath = Path.GetFullPath(storedSource);
        string storedPath = Path.GetFullPath(
            Path.Combine(storageDirectory, Path.GetFileName(suppliedPath)));
        if (!string.Equals(suppliedPath, storedPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("码表文件不在应用存储目录内。", nameof(storedSource));
        }

        return storedPath;
    }
}
