using CommunityToolkit.Mvvm.Input;
using TypingCore.Abstractions;

namespace TypingCore.Wpf.ViewModels;

/// <summary>
/// Presents the final statistics for one completed practice session.
/// </summary>
public sealed class PracticeResultViewModel : PageViewModel
{
    public PracticeResultViewModel(
        IArticleRecord article,
        IStatisticsSnapshot statistics,
        int errorCharacterCount,
        Action retry,
        Func<Task> showHistory,
        Action returnToLibrary)
        : base("练习结果")
    {
        ArgumentNullException.ThrowIfNull(article);
        ArgumentNullException.ThrowIfNull(statistics);
        ArgumentNullException.ThrowIfNull(retry);
        ArgumentNullException.ThrowIfNull(showHistory);
        ArgumentNullException.ThrowIfNull(returnToLibrary);

        ArticleTitle = article.Title;
        KeystrokesPerMinute = statistics.KeystrokesPerMinute;
        CharactersPerMinute = statistics.CharactersPerMinute;
        WordsPerMinute = statistics.WordsPerMinute;
        AverageCodeLength = statistics.AverageCodeLength;
        BackspaceCount = statistics.BackspaceCount;
        ErrorCharacterCount = errorCharacterCount;
        ErrorRate = statistics.ErrorRate;
        ElapsedText = FormatElapsed(statistics.Elapsed);

        RetryCommand = new RelayCommand(retry);
        ShowHistoryCommand = new AsyncRelayCommand(showHistory);
        ReturnToLibraryCommand = new RelayCommand(returnToLibrary);
    }

    public string ArticleTitle { get; }

    public double KeystrokesPerMinute { get; }

    public double CharactersPerMinute { get; }

    public double WordsPerMinute { get; }

    public double AverageCodeLength { get; }

    public int BackspaceCount { get; }

    public int ErrorCharacterCount { get; }

    public double ErrorRate { get; }

    public string ElapsedText { get; }

    public IRelayCommand RetryCommand { get; }

    public IAsyncRelayCommand ShowHistoryCommand { get; }

    public IRelayCommand ReturnToLibraryCommand { get; }

    private static string FormatElapsed(TimeSpan elapsed)
        => elapsed.TotalHours >= 1
            ? elapsed.ToString(@"hh\:mm\:ss")
            : elapsed.ToString(@"mm\:ss");
}
