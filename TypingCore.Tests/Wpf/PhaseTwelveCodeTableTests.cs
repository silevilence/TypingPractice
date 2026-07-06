using System.Runtime.ExceptionServices;
using System.Windows;
using TypingCore.Abstractions;
using TypingCore.Engine;
using TypingCore.Models;
using TypingCore.Parsing;
using TypingCore.Wpf.Services;
using TypingCore.Wpf.ViewModels;
using TypingCore.Wpf.Views;

namespace TypingCore.Tests.Wpf;

public sealed class PhaseTwelveCodeTableTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 6, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TypingPracticeViewModel_updates_hints_for_current_and_following_text()
    {
        CodeTableProvider provider = CreateProvider();
        TypingPracticeViewModel viewModel = new(
            new Article("article-1", "练习", "工人", Now, Array.Empty<string>()),
            new ArticleTextLayoutBuilder(),
            new FakeSystemClock(),
            () => { },
            codeTableProvider: provider);

        Assert.Equal(new[] { "工人", "工" }, viewModel.CodeHints.Select(hint => hint.SourceText));

        viewModel.HandleTextInput("工");

        ICodeLookupResult hint = Assert.Single(viewModel.CodeHints);
        Assert.Equal("人", hint.SourceText);
        Assert.Equal(new[] { "w" }, hint.CandidateCodes);
    }

    [Fact]
    public async Task CodeTableManagerViewModel_imports_activates_and_deletes_tables()
    {
        CodeTableProvider provider = new();
        CodeTable table = new(
            "五笔86",
            @"C:\tables\wubi.txt",
            Now,
            new[] { new CodeTableEntry("a", new[] { "工" }, 999) });
        FakeCodeTableRepository repository = new(importResult: table);
        CodeTableManagerViewModel viewModel = new(
            repository,
            provider,
            new FakeFileDialogService(@"C:\tables\wubi.txt"),
            new FakeSystemClock());

        await viewModel.ImportAsync();

        CodeTableItemViewModel item = Assert.Single(viewModel.CodeTables);
        Assert.True(item.IsActive);
        Assert.Same(table, provider.CurrentTable);
        Assert.Equal(table.Source, repository.ActiveSource);

        await item.DeleteCommand.ExecuteAsync(null);

        Assert.Empty(viewModel.CodeTables);
        Assert.Null(provider.CurrentTable);
        Assert.Empty(repository.Tables);
    }

    [Fact]
    public async Task CodeTableManagerViewModel_restores_active_table_on_load()
    {
        CodeTable table = CreateProvider().CurrentTable!;
        FakeCodeTableRepository repository = new(table, activeSource: table.Source);
        CodeTableProvider provider = new();
        CodeTableManagerViewModel viewModel = new(
            repository,
            provider,
            new FakeFileDialogService(@"C:\tables\wubi.txt"),
            new FakeSystemClock());

        await viewModel.LoadAsync();

        CodeTableItemViewModel item = Assert.Single(viewModel.CodeTables);
        Assert.True(item.IsActive);
        Assert.Same(table, provider.CurrentTable);
    }

    [Fact]
    public void MainViewModel_navigates_to_code_table_manager()
    {
        CodeTableProvider provider = new();
        CodeTableManagerViewModel manager = new(
            new FakeCodeTableRepository(),
            provider,
            new FakeFileDialogService(@"C:\tables\wubi.txt"),
            new FakeSystemClock());
        FakeArticleRepository articleRepository = new();
        InMemorySessionRepository sessionRepository = new();
        MainViewModel viewModel = new(
            new ArticleLibraryViewModel(
                articleRepository,
                new FakeArticleImportService(),
                new FakeFileDialogService(@"C:\articles\article.txt"),
                new FakeClipboardService(),
                new FakeSystemClock()),
            new HistoryViewModel(articleRepository, sessionRepository),
            new SettingsViewModel(),
            new ArticleTextLayoutBuilder(),
            new FakeSystemClock(),
            sessionRepository,
            manager,
            provider);

        viewModel.ShowCodeTableManagerCommand.Execute(null);

        Assert.Same(manager, viewModel.CurrentPage);
        Assert.True(viewModel.IsCodeTableManagerSelected);
    }

    [Fact]
    public void CodeTableManagerView_renders_read_only_entry_count_binding()
    {
        Exception? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                CodeTableManagerViewModel viewModel = new(
                    new FakeCodeTableRepository(),
                    new CodeTableProvider(),
                    new FakeFileDialogService(@"C:\tables\wubi.txt"),
                    new FakeSystemClock());
                viewModel.CodeTables.Add(new CodeTableItemViewModel(
                    CreateProvider().CurrentTable!,
                    _ => Task.CompletedTask,
                    _ => Task.CompletedTask));

                CodeTableManagerView view = new()
                {
                    DataContext = viewModel,
                };
                view.Measure(new Size(900d, 600d));
                view.Arrange(new Rect(0d, 0d, 900d, 600d));
                view.UpdateLayout();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private static CodeTableProvider CreateProvider()
    {
        CodeTableProvider provider = new();
        provider.Load(new CodeTable(
            "五笔86",
            "memory",
            Now,
            new[]
            {
                new CodeTableEntry("a", new[] { "工" }, 999),
                new CodeTableEntry("gg", new[] { "工人" }, 500),
                new CodeTableEntry("w", new[] { "人" }, 999),
            }));
        return provider;
    }

    private sealed class FakeCodeTableRepository(
        CodeTable? table = null,
        string? activeSource = null,
        CodeTable? importResult = null) : ICodeTableRepository
    {
        public List<CodeTable> Tables { get; } = table is null ? [] : [table];

        public string? ActiveSource { get; private set; } = activeSource;

        public Task<IReadOnlyList<CodeTable>> GetAllAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CodeTable>>(Tables.ToArray());

        public Task<CodeTable> ImportAsync(
            string sourceFilePath,
            DateTimeOffset loadedAt,
            CancellationToken cancellationToken = default)
        {
            CodeTable imported = importResult
                ?? new CodeTable(
                    Path.GetFileNameWithoutExtension(sourceFilePath),
                    sourceFilePath,
                    loadedAt,
                    new[] { new CodeTableEntry("a", new[] { "工" }, 999) });
            Tables.RemoveAll(existing =>
                string.Equals(existing.Source, imported.Source, StringComparison.OrdinalIgnoreCase));
            Tables.Add(imported);
            return Task.FromResult(imported);
        }

        public Task DeleteAsync(
            string storedSource,
            CancellationToken cancellationToken = default)
        {
            Tables.RemoveAll(table =>
                string.Equals(table.Source, storedSource, StringComparison.OrdinalIgnoreCase));
            if (string.Equals(ActiveSource, storedSource, StringComparison.OrdinalIgnoreCase))
            {
                ActiveSource = null;
            }

            return Task.CompletedTask;
        }

        public Task<string?> GetActiveSourceAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(ActiveSource);

        public Task SetActiveSourceAsync(
            string? storedSource,
            CancellationToken cancellationToken = default)
        {
            ActiveSource = storedSource;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFileDialogService(string filePath) : IFileDialogService
    {
        public string? SelectArticleFile() => null;

        public string? SelectCodeTableFile() => filePath;
    }

    private sealed class FakeSystemClock : ISystemClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeArticleRepository : IArticleRepository
    {
        public Task SaveAsync(IArticleRecord articleRecord, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(string articleId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IArticleRecord?> GetByIdAsync(
            string articleId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IArticleRecord?>(null);

        public Task<IReadOnlyList<IArticleRecord>> SearchAsync(
            string? query = null,
            string? tag = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<IArticleRecord>>([]);
    }

    private sealed class FakeArticleImportService : IArticleImportService
    {
        public ArticleImportResult ImportFromText(string title, string rawText)
            => new(title, new ArticleTextLayoutBuilder().Build(rawText), null);

        public Task<ArticleImportResult> ImportFromFileAsync(
            string filePath,
            CancellationToken cancellationToken = default)
            => Task.FromResult(ImportFromText("文章", string.Empty));
    }

    private sealed class FakeClipboardService : IClipboardService
    {
        public string? ReadText() => null;
    }
}
