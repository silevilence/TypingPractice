using CommunityToolkit.Mvvm.Input;

namespace TypingCore.Wpf.ViewModels;

/// <summary>
/// Represents a single article card in the article library list.
/// </summary>
public sealed class ArticleCardViewModel
{
    public ArticleCardViewModel(
        string articleId,
        string title,
        string previewText,
        string wordCountText,
        string createdAtText,
        IReadOnlyList<string> tags,
        IRelayCommand startPracticeCommand,
        IRelayCommand editCommand,
        IRelayCommand previewCommand,
        IRelayCommand deleteCommand,
        IRelayCommand<string> filterByTagCommand)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(articleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(previewText);
        ArgumentNullException.ThrowIfNull(wordCountText);
        ArgumentNullException.ThrowIfNull(createdAtText);
        ArgumentNullException.ThrowIfNull(tags);
        ArgumentNullException.ThrowIfNull(startPracticeCommand);
        ArgumentNullException.ThrowIfNull(editCommand);
        ArgumentNullException.ThrowIfNull(previewCommand);
        ArgumentNullException.ThrowIfNull(deleteCommand);
        ArgumentNullException.ThrowIfNull(filterByTagCommand);

        ArticleId = articleId;
        Title = title;
        PreviewText = previewText;
        WordCountText = wordCountText;
        CreatedAtText = createdAtText;
        Tags = tags;
        StartPracticeCommand = startPracticeCommand;
        EditCommand = editCommand;
        PreviewCommand = previewCommand;
        DeleteCommand = deleteCommand;
        FilterByTagCommand = filterByTagCommand;
    }

    public string ArticleId { get; }

    public string Title { get; }

    public string PreviewText { get; }

    public string WordCountText { get; }

    public string CreatedAtText { get; }

    public IReadOnlyList<string> Tags { get; }

    public IRelayCommand StartPracticeCommand { get; }

    public IRelayCommand EditCommand { get; }

    public IRelayCommand PreviewCommand { get; }

    public IRelayCommand DeleteCommand { get; }

    public IRelayCommand<string> FilterByTagCommand { get; }
}
