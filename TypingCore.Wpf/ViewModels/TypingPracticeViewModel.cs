using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TypingCore.Abstractions;
using TypingCore.Engine;
using TypingCore.Models;
using TypingCore.Parsing;
using TypingCore.Wpf.Services;

namespace TypingCore.Wpf.ViewModels;

/// <summary>
/// Hosts the stage-eight practice session and adapts WPF keyboard or IME events into the core typing engine.
/// </summary>
public sealed class TypingPracticeViewModel : PageViewModel
{
    private readonly IArticleRecord article;
    private readonly Action<IArticleRecord, IStatisticsSnapshot, int>? completionCallback;
    private readonly WindowMessageInputTranslator inputTranslator;
    private readonly Action returnToLibrary;
    private readonly ITypingSession session;
    private readonly ISessionRepository? sessionRepository;
    private readonly ISystemClock systemClock;
    private double averageCodeLength;
    private IReadOnlyList<TypingCharacterSnapshot> characterSnapshots = [];
    private double charactersPerMinute;
    private string committedText = string.Empty;
    private int correctCharacterCount;
    private int completionVersion;
    private int currentTextIndex;
    private int errorCharacterCount;
    private bool isCompletionHandled;
    private bool isInterleavedLayout = true;
    private double keystrokesPerMinute;
    private string sessionId = Guid.NewGuid().ToString("N");
    private DateTimeOffset? sessionStartedAt;
    private TypingSessionState sessionState;
    private string statusMessage;

    public TypingPracticeViewModel(
        IArticleRecord article,
        IArticleTextLayoutBuilder articleTextLayoutBuilder,
        ISystemClock systemClock,
        Action returnToLibrary,
        ISessionRepository? sessionRepository = null,
        Action<IArticleRecord, IStatisticsSnapshot, int>? completionCallback = null)
        : base("开始练习")
    {
        ArgumentNullException.ThrowIfNull(article);
        ArgumentNullException.ThrowIfNull(articleTextLayoutBuilder);
        ArgumentNullException.ThrowIfNull(systemClock);
        ArgumentNullException.ThrowIfNull(returnToLibrary);

        this.article = article;
        this.completionCallback = completionCallback;
        this.returnToLibrary = returnToLibrary;
        this.sessionRepository = sessionRepository;
        this.systemClock = systemClock;

        ArticleTitle = article.Title;
        ArticleLayout = articleTextLayoutBuilder.Build(article.RawText);
        TargetText = ArticleLayout.NormalizedText;
        session = new TypingSession(ArticleLayout);
        inputTranslator = new WindowMessageInputTranslator(systemClock);
        statusMessage = "已进入练习页，直接开始输入即可。可随时切换逐字对齐或上下跟随布局。";

        RestartCommand = new RelayCommand(Restart);
        ReturnToLibraryCommand = new RelayCommand(ReturnToLibrary);
        SelectInterleavedLayoutCommand = new RelayCommand(() => SelectLayout(true));
        SelectFollowingLayoutCommand = new RelayCommand(() => SelectLayout(false));

        RefreshSessionState();
    }

    public string ArticleTitle { get; }

    public ArticleTextLayout ArticleLayout { get; }

    public string TargetText { get; }

    public IReadOnlyList<TypingCharacterSnapshot> CharacterSnapshots
    {
        get => characterSnapshots;
        private set => SetProperty(ref characterSnapshots, value);
    }

    public string CommittedText
    {
        get => committedText;
        private set => SetProperty(ref committedText, value);
    }

    public int CurrentTextIndex
    {
        get => currentTextIndex;
        private set
        {
            if (SetProperty(ref currentTextIndex, value))
            {
                OnPropertyChanged(nameof(ProgressText));
            }
        }
    }

    public int CorrectCharacterCount
    {
        get => correctCharacterCount;
        private set => SetProperty(ref correctCharacterCount, value);
    }

    public int ErrorCharacterCount
    {
        get => errorCharacterCount;
        private set => SetProperty(ref errorCharacterCount, value);
    }

    public double KeystrokesPerMinute
    {
        get => keystrokesPerMinute;
        private set => SetProperty(ref keystrokesPerMinute, value);
    }

    public double CharactersPerMinute
    {
        get => charactersPerMinute;
        private set => SetProperty(ref charactersPerMinute, value);
    }

    public double AverageCodeLength
    {
        get => averageCodeLength;
        private set => SetProperty(ref averageCodeLength, value);
    }

    public TypingSessionState SessionState
    {
        get => sessionState;
        private set
        {
            if (SetProperty(ref sessionState, value))
            {
                OnPropertyChanged(nameof(IsCompleted));
            }
        }
    }

    public bool IsCompleted => SessionState == TypingSessionState.Completed;

    public bool IsInterleavedLayout
    {
        get => isInterleavedLayout;
        private set
        {
            if (SetProperty(ref isInterleavedLayout, value))
            {
                OnPropertyChanged(nameof(IsFollowingLayout));
            }
        }
    }

    public bool IsFollowingLayout => !IsInterleavedLayout;

    public string ProgressText => $"{CurrentTextIndex} / {TargetText.Length}";

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetProperty(ref statusMessage, value);
    }

    public Task CompletionTask { get; private set; } = Task.CompletedTask;

    public IRelayCommand RestartCommand { get; }

    public IRelayCommand ReturnToLibraryCommand { get; }

    public IRelayCommand SelectInterleavedLayoutCommand { get; }

    public IRelayCommand SelectFollowingLayoutCommand { get; }

    public bool HandleWindowMessage(int message, nint wParam)
    {
        IKeyInputEvent? inputEvent = inputTranslator.TranslateWindowMessage(message, wParam);
        return inputEvent is not null && HandleNormalizedInput(inputEvent);
    }

    public bool HandlePreviewKeyDown(int virtualKey)
    {
        if (virtualKey == 0x0D)
        {
            return IsAwaitingLineBreakInput()
                ? HandleTextInput("\n")
                : false;
        }

        return HandleWindowMessage(WindowMessageInputTranslator.WmKeyDown, (nint)virtualKey);
    }

    public bool HandleTextInput(string text)
    {
        IKeyInputEvent? inputEvent = inputTranslator.TranslateTextInput(text);
        return inputEvent is not null && HandleNormalizedInput(inputEvent);
    }

    private bool HandleNormalizedInput(IKeyInputEvent inputEvent)
    {
        switch (inputEvent.Key)
        {
            case KeyInputKey.Escape:
                Restart();
                return true;
            case KeyInputKey.LeftArrow:
            case KeyInputKey.RightArrow:
            case KeyInputKey.UpArrow:
            case KeyInputKey.DownArrow:
                StatusMessage = "方向键已预留给后续排版与定位阶段，当前不会移动光标。";
                return true;
        }

        if (session.Snapshot.State == TypingSessionState.NotStarted)
        {
            sessionStartedAt = inputEvent.Timestamp;
        }

        bool wasCompleted = IsCompleted;
        session.ProcessInput(inputEvent);
        RefreshSessionState();

        if (wasCompleted && !IsCompleted)
        {
            isCompletionHandled = false;
            completionVersion++;
        }

        if (IsCompleted && !isCompletionHandled)
        {
            isCompletionHandled = true;
            int queuedCompletionVersion = ++completionVersion;
            IStatisticsSnapshot statistics = session.StatisticsProvider.Current;
            CompletionTask = SaveCompletedSessionAsync(
                sessionId,
                sessionStartedAt ?? systemClock.UtcNow,
                statistics,
                ErrorCharacterCount,
                queuedCompletionVersion);
        }

        if (IsAwaitingLineBreakInput())
        {
            StatusMessage = "当前行已完成，按 Enter 进入下一行。";
            return true;
        }

        if (inputEvent.IsBackspace)
        {
            StatusMessage = CurrentTextIndex == 0
                ? "已回退到当前起点。"
                : "已删除上一个已上屏字符。";
            return true;
        }

        if (!string.IsNullOrEmpty(inputEvent.ImeCommitText))
        {
            StatusMessage = IsCompleted
                ? $"《{ArticleTitle}》已完成，按 Esc 可以重新开始。"
                : string.Equals(inputEvent.ImeCommitText, "\n", StringComparison.Ordinal)
                    ? "已进入下一行，继续输入即可。"
                : inputEvent.IsFromIme
                    ? "已接收输入法上屏内容。"
                    : "已接收当前输入。";
        }

        return inputEvent.Key != KeyInputKey.Unknown || !string.IsNullOrEmpty(inputEvent.ImeCommitText);
    }

    private bool IsAwaitingLineBreakInput()
        => !IsCompleted
            && CurrentTextIndex >= 0
            && CurrentTextIndex < CharacterSnapshots.Count
            && CharacterSnapshots[CurrentTextIndex].TargetChar == '\n'
            && CharacterSnapshots[CurrentTextIndex].State == TypingCharacterState.Current;

    private void Restart()
    {
        session.Reset();
        inputTranslator.Reset();
        isCompletionHandled = false;
        completionVersion++;
        sessionId = Guid.NewGuid().ToString("N");
        sessionStartedAt = null;
        CompletionTask = Task.CompletedTask;
        RefreshSessionState();
        StatusMessage = "已重新开始当前文章。";
    }

    private void ReturnToLibrary()
    {
        completionVersion++;
        returnToLibrary();
    }

    private void SelectLayout(bool useInterleavedLayout)
    {
        if (IsInterleavedLayout == useInterleavedLayout)
        {
            return;
        }

        IsInterleavedLayout = useInterleavedLayout;
        StatusMessage = useInterleavedLayout
            ? "已切换到逐字对齐布局，当前练习进度保持不变。"
            : "已切换到上下跟随布局，当前练习进度保持不变。";
    }

    private void RefreshSessionState()
    {
        ITypingSessionSnapshot snapshot = session.Snapshot;
        IStatisticsSnapshot statistics = session.StatisticsProvider.Current;
        CharacterSnapshots = snapshot.Characters;
        SessionState = snapshot.State;
        CurrentTextIndex = snapshot.CurrentTextIndex;
        CorrectCharacterCount = snapshot.CorrectCharacterCount;
        ErrorCharacterCount = snapshot.ErrorCharacterCount;
        KeystrokesPerMinute = statistics.KeystrokesPerMinute;
        CharactersPerMinute = statistics.CharactersPerMinute;
        AverageCodeLength = statistics.AverageCodeLength;
        CommittedText = new string(snapshot.Characters
            .Take(CurrentTextIndex)
            .Select(character => character.InputChar ?? '\0')
            .Where(character => character != '\0')
            .ToArray());
    }

    private async Task SaveCompletedSessionAsync(
        string completedSessionId,
        DateTimeOffset startedAt,
        IStatisticsSnapshot statistics,
        int completedErrorCount,
        int queuedCompletionVersion)
    {
        try
        {
            if (sessionRepository is not null)
            {
                await sessionRepository.SaveAsync(new TypingSessionRecord(
                    completedSessionId,
                    article.ArticleId,
                    startedAt,
                    systemClock.UtcNow,
                    statistics));
            }

            if (sessionId == completedSessionId
                && completionVersion == queuedCompletionVersion
                && IsCompleted)
            {
                completionCallback?.Invoke(article, statistics, completedErrorCount);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"练习已完成，但记录保存失败：{ex.Message}";
        }
    }
}
