using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using TypingCore.Abstractions;

namespace TypingCore.Wpf.ViewModels;

/// <summary>
/// Loads completed practice records by article and exposes their historical trends.
/// </summary>
public sealed class HistoryViewModel : PageViewModel
{
    private static readonly SKColor AccentColor = new(0x8B, 0x5E, 0x3C);
    private static readonly SKColor ErrorColor = new(0xC7, 0x51, 0x46);
    private static readonly SKColor SecondaryTextColor = new(0x7A, 0x70, 0x67);
    private static readonly SKColor BorderColor = new(0xE8, 0xE2, 0xDA);

    private readonly IArticleRepository articleRepository;
    private readonly ISessionRepository sessionRepository;
    private Axis[] errorRateXAxes = [];
    private ISeries[] errorRateSeries = [];
    private Axis[] errorRateYAxes = [];
    private bool isBusy;
    private HistoryArticleOption? selectedArticle;
    private Axis[] speedXAxes = [];
    private ISeries[] speedSeries = [];
    private Axis[] speedYAxes = [];
    private string statusMessage = "选择文章后查看历次练习记录与趋势。";

    public HistoryViewModel(
        IArticleRepository articleRepository,
        ISessionRepository sessionRepository)
        : base("历史记录")
    {
        this.articleRepository = articleRepository ?? throw new ArgumentNullException(nameof(articleRepository));
        this.sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));

        Articles = new ObservableCollection<HistoryArticleOption>();
        Records = new ObservableCollection<HistoryRecordViewModel>();
        SelectArticleCommand = new AsyncRelayCommand<HistoryArticleOption>(
            SelectArticleAsync,
            _ => !IsBusy);
        RefreshCommand = new AsyncRelayCommand(
            () => LoadAsync(SelectedArticle?.ArticleId),
            () => !IsBusy);
    }

    public ObservableCollection<HistoryArticleOption> Articles { get; }

    public ObservableCollection<HistoryRecordViewModel> Records { get; }

    public HistoryArticleOption? SelectedArticle
    {
        get => selectedArticle;
        private set
        {
            if (SetProperty(ref selectedArticle, value))
            {
                OnPropertyChanged(nameof(SelectedArticleTitle));
            }
        }
    }

    public string SelectedArticleTitle => SelectedArticle?.Title ?? "尚未选择文章";

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                RefreshCommand.NotifyCanExecuteChanged();
                SelectArticleCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetProperty(ref statusMessage, value);
    }

    public ISeries[] SpeedSeries
    {
        get => speedSeries;
        private set => SetProperty(ref speedSeries, value);
    }

    public Axis[] SpeedXAxes
    {
        get => speedXAxes;
        private set => SetProperty(ref speedXAxes, value);
    }

    public Axis[] SpeedYAxes
    {
        get => speedYAxes;
        private set => SetProperty(ref speedYAxes, value);
    }

    public ISeries[] ErrorRateSeries
    {
        get => errorRateSeries;
        private set => SetProperty(ref errorRateSeries, value);
    }

    public Axis[] ErrorRateXAxes
    {
        get => errorRateXAxes;
        private set => SetProperty(ref errorRateXAxes, value);
    }

    public Axis[] ErrorRateYAxes
    {
        get => errorRateYAxes;
        private set => SetProperty(ref errorRateYAxes, value);
    }

    public IAsyncRelayCommand<HistoryArticleOption> SelectArticleCommand { get; }

    public IAsyncRelayCommand RefreshCommand { get; }

    public async Task LoadAsync(string? preferredArticleId = null)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            IReadOnlyList<IArticleRecord> articles = await articleRepository.SearchAsync();

            Articles.Clear();
            foreach (IArticleRecord article in articles)
            {
                Articles.Add(new HistoryArticleOption(article.ArticleId, article.Title));
            }

            HistoryArticleOption? preferred = Articles.FirstOrDefault(
                item => string.Equals(item.ArticleId, preferredArticleId, StringComparison.Ordinal));
            SelectedArticle = preferred ?? Articles.FirstOrDefault();

            if (SelectedArticle is null)
            {
                ClearRecords();
                StatusMessage = "当前还没有文章，完成文章导入后再查看历史记录。";
                return;
            }

            await LoadSelectedArticleAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"历史记录加载失败：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SelectArticleAsync(HistoryArticleOption? article)
    {
        if (article is null || IsBusy)
        {
            return;
        }

        SelectedArticle = article;

        try
        {
            IsBusy = true;
            await LoadSelectedArticleAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"历史记录加载失败：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadSelectedArticleAsync()
    {
        if (SelectedArticle is null)
        {
            ClearRecords();
            return;
        }

        IReadOnlyList<ISessionRecord> sessions =
            await sessionRepository.GetByArticleIdAsync(SelectedArticle.ArticleId);
        ISessionRecord[] completedSessions = sessions
            .Where(session => session.EndedAt.HasValue && session.Statistics is not null)
            .ToArray();

        Records.Clear();
        foreach (ISessionRecord session in completedSessions)
        {
            Records.Add(new HistoryRecordViewModel(session));
        }

        BuildCharts(completedSessions.Reverse().ToArray());
        StatusMessage = completedSessions.Length == 0
            ? $"《{SelectedArticle.Title}》还没有已完成的练习记录。"
            : $"《{SelectedArticle.Title}》共有 {completedSessions.Length} 次已完成练习。";
    }

    private void BuildCharts(IReadOnlyList<ISessionRecord> sessions)
    {
        string[] labels = sessions
            .Select(session => session.StartedAt.LocalDateTime.ToString("MM-dd HH:mm"))
            .ToArray();
        double[] speedValues = sessions
            .Select(session => session.Statistics!.CharactersPerMinute)
            .ToArray();
        double[] errorValues = sessions
            .Select(session => session.Statistics!.ErrorRate * 100d)
            .ToArray();

        SpeedSeries =
        [
            CreateLineSeries("字速", speedValues, AccentColor),
        ];
        ErrorRateSeries =
        [
            CreateLineSeries("错误率", errorValues, ErrorColor),
        ];
        SpeedXAxes = [CreateXAxis(labels)];
        ErrorRateXAxes = [CreateXAxis(labels)];
        SpeedYAxes = [CreateYAxis(value => $"{value:0}")];
        ErrorRateYAxes = [CreateYAxis(value => $"{value:0}%")];
    }

    private void ClearRecords()
    {
        Records.Clear();
        SpeedSeries = [];
        SpeedXAxes = [];
        SpeedYAxes = [];
        ErrorRateSeries = [];
        ErrorRateXAxes = [];
        ErrorRateYAxes = [];
    }

    private static LineSeries<double> CreateLineSeries(
        string name,
        IReadOnlyCollection<double> values,
        SKColor color)
        => new()
        {
            Name = name,
            Values = values,
            Fill = null,
            GeometrySize = 8,
            LineSmoothness = 0.35,
            Stroke = new SolidColorPaint(color, 2),
            GeometryFill = new SolidColorPaint(color),
            GeometryStroke = new SolidColorPaint(color, 2),
        };

    private static Axis CreateXAxis(IList<string> labels)
        => new()
        {
            Labels = labels,
            LabelsPaint = new SolidColorPaint(SecondaryTextColor),
            SeparatorsPaint = new SolidColorPaint(BorderColor, 1),
            TextSize = 12,
        };

    private static Axis CreateYAxis(Func<double, string> labeler)
        => new()
        {
            Labeler = labeler,
            LabelsPaint = new SolidColorPaint(SecondaryTextColor),
            SeparatorsPaint = new SolidColorPaint(BorderColor, 1),
            TextSize = 12,
            MinLimit = 0,
        };
}

public sealed record HistoryArticleOption(string ArticleId, string Title);

public sealed class HistoryRecordViewModel
{
    public HistoryRecordViewModel(ISessionRecord session)
    {
        ArgumentNullException.ThrowIfNull(session);
        IStatisticsSnapshot statistics = session.Statistics
            ?? throw new ArgumentException("A completed history row requires statistics.", nameof(session));

        StartedAtText = session.StartedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm");
        CharactersPerMinute = statistics.CharactersPerMinute;
        KeystrokesPerMinute = statistics.KeystrokesPerMinute;
        AverageCodeLength = statistics.AverageCodeLength;
        ErrorRate = statistics.ErrorRate;
        BackspaceCount = statistics.BackspaceCount;
        ElapsedText = statistics.Elapsed.TotalHours >= 1
            ? statistics.Elapsed.ToString(@"hh\:mm\:ss")
            : statistics.Elapsed.ToString(@"mm\:ss");
    }

    public string StartedAtText { get; }

    public double CharactersPerMinute { get; }

    public double KeystrokesPerMinute { get; }

    public double AverageCodeLength { get; }

    public double ErrorRate { get; }

    public int BackspaceCount { get; }

    public string ElapsedText { get; }
}
