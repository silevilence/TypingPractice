using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TypingCore.Models;

namespace TypingCore.Wpf.ViewModels;

/// <summary>
/// Presents one imported code table and its management commands.
/// </summary>
public sealed class CodeTableItemViewModel : ObservableObject
{
    private bool isActive;

    internal CodeTableItemViewModel(
        CodeTable table,
        Func<CodeTableItemViewModel, Task> activate,
        Func<CodeTableItemViewModel, Task> delete)
    {
        Table = table ?? throw new ArgumentNullException(nameof(table));
        ActivateCommand = new AsyncRelayCommand(() => activate(this));
        DeleteCommand = new AsyncRelayCommand(() => delete(this));
    }

    internal CodeTable Table { get; }

    public string Name => Table.Name;

    public string Source => Table.Source;

    public int EntryCount => Table.Entries.Count;

    public string LoadedAtText => Table.LoadedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm");

    public bool IsActive
    {
        get => isActive;
        internal set => SetProperty(ref isActive, value);
    }

    public IAsyncRelayCommand ActivateCommand { get; }

    public IAsyncRelayCommand DeleteCommand { get; }
}
