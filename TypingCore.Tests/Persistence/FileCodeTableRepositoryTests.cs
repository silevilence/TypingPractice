using TypingCore.Models;
using TypingCore.Parsing;
using TypingCore.Persistence;

namespace TypingCore.Tests.Persistence;

public sealed class FileCodeTableRepositoryTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"TypingCore.CodeTables.{Guid.NewGuid():N}");

    [Fact]
    public async Task ImportAsync_restores_table_and_active_selection_after_source_is_removed()
    {
        string sourceDirectory = Path.Combine(tempDirectory, "source");
        string storageDirectory = Path.Combine(tempDirectory, "stored");
        Directory.CreateDirectory(sourceDirectory);
        string sourcePath = Path.Combine(sourceDirectory, "wubi06.dict");
        await File.WriteAllTextAsync(sourcePath, "a\t工\t999");
        DateTimeOffset loadedAt = new(2026, 7, 6, 10, 0, 0, TimeSpan.Zero);

        FileCodeTableRepository repository = new(storageDirectory, new CodeTableParser());
        CodeTable imported = await repository.ImportAsync(sourcePath, loadedAt);
        await repository.SetActiveSourceAsync(imported.Source);
        File.Delete(sourcePath);

        FileCodeTableRepository restartedRepository = new(storageDirectory, new CodeTableParser());
        CodeTable restored = Assert.Single(await restartedRepository.GetAllAsync());

        Assert.Equal("wubi06", restored.Name);
        Assert.Equal("工", Assert.Single(Assert.Single(restored.Entries).Candidates));
        Assert.Equal(imported.Source, await restartedRepository.GetActiveSourceAsync());
        Assert.True(File.Exists(restored.Source));
    }

    [Fact]
    public async Task DeleteAsync_removes_stored_table_and_active_selection()
    {
        string sourceDirectory = Path.Combine(tempDirectory, "source");
        string storageDirectory = Path.Combine(tempDirectory, "stored");
        Directory.CreateDirectory(sourceDirectory);
        string sourcePath = Path.Combine(sourceDirectory, "wubi06.dict");
        await File.WriteAllTextAsync(sourcePath, "a\t工\t999");
        FileCodeTableRepository repository = new(storageDirectory, new CodeTableParser());
        CodeTable imported = await repository.ImportAsync(
            sourcePath,
            new DateTimeOffset(2026, 7, 6, 10, 0, 0, TimeSpan.Zero));
        await repository.SetActiveSourceAsync(imported.Source);

        await repository.DeleteAsync(imported.Source);

        Assert.Empty(await repository.GetAllAsync());
        Assert.Null(await repository.GetActiveSourceAsync());
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
