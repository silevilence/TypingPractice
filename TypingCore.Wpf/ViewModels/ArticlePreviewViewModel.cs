using CommunityToolkit.Mvvm.Input;
using TypingCore.Abstractions;
using TypingCore.Models;
using TypingCore.Parsing;

namespace TypingCore.Wpf.ViewModels;

/// <summary>
/// Shows a read-only article preview using the same layout data as typing practice.
/// </summary>
/// <remarks>
/// Instances are intended for WPF UI-thread use.
/// </remarks>
public sealed class ArticlePreviewViewModel : PageViewModel
{
    private readonly IArticleRecord article;
    private readonly Action<IArticleRecord> startPractice;
    private readonly Action<IArticleRecord> edit;
    private readonly Action back;

    public ArticlePreviewViewModel(
        IArticleRecord article,
        IArticleTextLayoutBuilder articleTextLayoutBuilder,
        Action<IArticleRecord> startPractice,
        Action<IArticleRecord> edit,
        Action back)
        : base("文章预览")
    {
        this.article = article ?? throw new ArgumentNullException(nameof(article));
        ArgumentNullException.ThrowIfNull(articleTextLayoutBuilder);
        this.startPractice = startPractice ?? throw new ArgumentNullException(nameof(startPractice));
        this.edit = edit ?? throw new ArgumentNullException(nameof(edit));
        this.back = back ?? throw new ArgumentNullException(nameof(back));

        ArticleTitle = article.Title;
        CreatedAtText = article.CreatedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm");
        Tags = article.Tags.ToArray();
        ArticleLayout = articleTextLayoutBuilder.Build(article.RawText);
        CharacterSnapshots = ArticleLayout.Characters
            .Select(character => new TypingCharacterSnapshot(
                character.Index,
                character.Value,
                null,
                TypingCharacterState.Pending))
            .ToArray();

        StartPracticeCommand = new RelayCommand(() => this.startPractice(this.article));
        EditCommand = new RelayCommand(() => this.edit(this.article));
        BackCommand = new RelayCommand(this.back);
    }

    public string ArticleTitle { get; }

    public string CreatedAtText { get; }

    public IReadOnlyList<string> Tags { get; }

    public ArticleTextLayout ArticleLayout { get; }

    public IReadOnlyList<TypingCharacterSnapshot> CharacterSnapshots { get; }

    public int CurrentTextIndex => 0;

    public IRelayCommand StartPracticeCommand { get; }

    public IRelayCommand EditCommand { get; }

    public IRelayCommand BackCommand { get; }
}
