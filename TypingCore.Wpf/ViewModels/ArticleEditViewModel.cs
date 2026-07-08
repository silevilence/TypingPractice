using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TypingCore.Abstractions;
using TypingCore.Models;
using TypingCore.Parsing;

namespace TypingCore.Wpf.ViewModels;

/// <summary>
/// Edits an existing article and persists the normalized article text.
/// </summary>
/// <remarks>
/// Instances are intended for WPF UI-thread use.
/// </remarks>
public sealed class ArticleEditViewModel : PageViewModel
{
    private readonly IArticleTextLayoutBuilder articleTextLayoutBuilder;
    private readonly IArticleRepository articleRepository;
    private readonly Func<IArticleRecord, Task> saved;
    private readonly Action cancelled;
    private IArticleRecord article;
    private string bodyText;
    private bool isBusy;
    private string statusMessage = "修改标题、标签或正文后保存。正文会重新分字，确保练习布局使用最新内容。";
    private string tagsText;
    private string titleText;

    public ArticleEditViewModel(
        IArticleRecord article,
        IArticleRepository articleRepository,
        IArticleTextLayoutBuilder articleTextLayoutBuilder,
        Func<IArticleRecord, Task> saved,
        Action cancelled)
        : base("编辑文章")
    {
        this.article = article ?? throw new ArgumentNullException(nameof(article));
        this.articleRepository = articleRepository ?? throw new ArgumentNullException(nameof(articleRepository));
        this.articleTextLayoutBuilder = articleTextLayoutBuilder ?? throw new ArgumentNullException(nameof(articleTextLayoutBuilder));
        this.saved = saved ?? throw new ArgumentNullException(nameof(saved));
        this.cancelled = cancelled ?? throw new ArgumentNullException(nameof(cancelled));
        titleText = article.Title;
        tagsText = string.Join("，", article.Tags);
        bodyText = article.RawText;

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
        CancelCommand = new RelayCommand(this.cancelled);
    }

    public string TitleText
    {
        get => titleText;
        set => SetProperty(ref titleText, value);
    }

    public string TagsText
    {
        get => tagsText;
        set => SetProperty(ref tagsText, value);
    }

    public string BodyText
    {
        get => bodyText;
        set => SetProperty(ref bodyText, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetProperty(ref statusMessage, value);
    }

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                SaveCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public IAsyncRelayCommand SaveCommand { get; }

    public IRelayCommand CancelCommand { get; }

    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            string title = TitleText.Trim();
            if (title.Length == 0)
            {
                StatusMessage = "标题不能为空。";
                return;
            }

            ArticleTextLayout layout = articleTextLayoutBuilder.Build(BodyText);
            if (string.IsNullOrWhiteSpace(layout.NormalizedText))
            {
                StatusMessage = "正文不能为空。";
                return;
            }

            Article updated = new(
                article.ArticleId,
                title,
                layout.NormalizedText,
                article.CreatedAt,
                ParseTags(TagsText));

            await articleRepository.UpdateAsync(updated).ConfigureAwait(false);
            article = updated;
            StatusMessage = $"已保存《{updated.Title}》。";
            await saved(updated).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存失败：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string[] ParseTags(string rawTags)
        => rawTags
            .Split([',', '，', ';', '；', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
