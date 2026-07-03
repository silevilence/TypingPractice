using CommunityToolkit.Mvvm.Input;

namespace TypingCore.Wpf.ViewModels;

/// <summary>
/// Represents a single article card in the article library list.
/// </summary>
public sealed class ArticleCardViewModel
{
    public ArticleCardViewModel(
        string title,
        string previewText,
        string createdAtText,
        IReadOnlyList<string> tags,
        IRelayCommand startPracticeCommand)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(previewText);
        ArgumentNullException.ThrowIfNull(createdAtText);
        ArgumentNullException.ThrowIfNull(tags);
        ArgumentNullException.ThrowIfNull(startPracticeCommand);

        Title = title;
        PreviewText = previewText;
        CreatedAtText = createdAtText;
        Tags = tags;
        StartPracticeCommand = startPracticeCommand;
    }

    public string Title { get; }

    public string PreviewText { get; }

    public string CreatedAtText { get; }

    public IReadOnlyList<string> Tags { get; }

    public IRelayCommand StartPracticeCommand { get; }
}