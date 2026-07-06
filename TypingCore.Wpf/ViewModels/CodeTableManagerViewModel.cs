using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TypingCore.Abstractions;
using TypingCore.Engine;
using TypingCore.Models;
using TypingCore.Wpf.Services;

namespace TypingCore.Wpf.ViewModels;

/// <summary>
/// Imports, restores, activates, and removes persisted code tables.
/// </summary>
public sealed class CodeTableManagerViewModel : PageViewModel
{
    private readonly ICodeTableRepository repository;
    private readonly CodeTableProvider provider;
    private readonly IFileDialogService fileDialogService;
    private readonly ISystemClock systemClock;
    private bool isBusy;
    private string statusMessage = "导入文本码表后，练习页会显示当前位置的编码提示。";

    public CodeTableManagerViewModel(
        ICodeTableRepository repository,
        CodeTableProvider provider,
        IFileDialogService fileDialogService,
        ISystemClock systemClock)
        : base("码表管理")
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        this.fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        this.systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));

        CodeTables = new ObservableCollection<CodeTableItemViewModel>();
        ImportCommand = new AsyncRelayCommand(ImportAsync, () => !IsBusy);
    }

    public ObservableCollection<CodeTableItemViewModel> CodeTables { get; }

    public IAsyncRelayCommand ImportCommand { get; }

    public bool HasCodeTables => CodeTables.Count > 0;

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                ImportCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetProperty(ref statusMessage, value);
    }

    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            IReadOnlyList<CodeTable> tables = await repository.GetAllAsync();
            string? activeSource = await repository.GetActiveSourceAsync();

            CodeTables.Clear();
            foreach (CodeTable table in tables)
            {
                CodeTables.Add(CreateItem(table));
            }

            CodeTableItemViewModel? activeItem = CodeTables.FirstOrDefault(item =>
                string.Equals(item.Source, activeSource, StringComparison.OrdinalIgnoreCase));
            if (activeItem is null)
            {
                provider.Clear();
                StatusMessage = CodeTables.Count == 0
                    ? "尚未导入码表。"
                    : $"已恢复 {CodeTables.Count} 个码表，请选择要启用的码表。";
            }
            else
            {
                provider.Load(activeItem.Table);
                activeItem.IsActive = true;
                StatusMessage = $"已恢复当前码表：{activeItem.Name}。";
            }

            OnPropertyChanged(nameof(HasCodeTables));
        }
        catch (Exception ex)
        {
            StatusMessage = $"码表恢复失败：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ImportAsync()
    {
        if (IsBusy)
        {
            return;
        }

        string? filePath = fileDialogService.SelectCodeTableFile();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            StatusMessage = "已取消码表导入。";
            return;
        }

        try
        {
            IsBusy = true;
            CodeTable table = await repository.ImportAsync(filePath, systemClock.UtcNow);
            CodeTableItemViewModel? existing = CodeTables.FirstOrDefault(
                item => string.Equals(item.Source, table.Source, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                CodeTables.Remove(existing);
            }

            CodeTableItemViewModel item = CreateItem(table);
            CodeTables.Add(item);
            OnPropertyChanged(nameof(HasCodeTables));
            await ActivateAsync(item);
            StatusMessage = $"已导入并启用“{table.Name}”，共 {table.Entries.Count} 条记录。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"码表导入失败：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private CodeTableItemViewModel CreateItem(CodeTable table)
        => new(table, ActivateAsync, DeleteAsync);

    private async Task ActivateAsync(CodeTableItemViewModel item)
    {
        try
        {
            await repository.SetActiveSourceAsync(item.Source);
            provider.Load(item.Table);

            foreach (CodeTableItemViewModel codeTable in CodeTables)
            {
                codeTable.IsActive = ReferenceEquals(codeTable, item);
            }

            StatusMessage = $"当前码表：{item.Name}。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"码表切换失败：{ex.Message}";
        }
    }

    private async Task DeleteAsync(CodeTableItemViewModel item)
    {
        try
        {
            bool wasActive = item.IsActive;
            await repository.DeleteAsync(item.Source);
            CodeTables.Remove(item);
            OnPropertyChanged(nameof(HasCodeTables));

            if (wasActive)
            {
                provider.Clear();
            }

            StatusMessage = wasActive
                ? $"已删除“{item.Name}”，当前没有启用码表。"
                : $"已删除“{item.Name}”。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"码表删除失败：{ex.Message}";
        }
    }
}
