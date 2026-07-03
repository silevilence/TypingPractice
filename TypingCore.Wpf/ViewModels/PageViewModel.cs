using CommunityToolkit.Mvvm.ComponentModel;

namespace TypingCore.Wpf.ViewModels;

/// <summary>
/// Base type for top-level content pages shown inside the main window shell.
/// </summary>
public abstract class PageViewModel : ObservableObject
{
    protected PageViewModel(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        DisplayName = displayName;
    }

    /// <summary>
    /// Gets the page title shown by the host shell.
    /// </summary>
    public string DisplayName { get; }
}