namespace TypingCore.Wpf.ViewModels;

/// <summary>
/// Represents a single article card in the article library list.
/// </summary>
public sealed class ArticleCardViewModel
{
    public ArticleCardViewModel(string title, string previewText, string createdAtText, IReadOnlyList<string> tags)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(previewText);
        ArgumentNullException.ThrowIfNull(createdAtText);
        ArgumentNullException.ThrowIfNull(tags);

        Title = title;
        PreviewText = previewText;
        CreatedAtText = createdAtText;
        Tags = tags;
    }

    public string Title { get; }

    public string PreviewText { get; }

    public string CreatedAtText { get; }

    public IReadOnlyList<string> Tags { get; }
}