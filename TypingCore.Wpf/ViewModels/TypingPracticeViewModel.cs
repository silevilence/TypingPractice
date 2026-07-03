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
    private readonly WindowMessageInputTranslator inputTranslator;
    private readonly Action returnToLibrary;
    private readonly ITypingSession session;
    private string committedText = string.Empty;
    private int correctCharacterCount;
    private int currentTextIndex;
    private int errorCharacterCount;
    private TypingSessionState sessionState;
    private string statusMessage;

    public TypingPracticeViewModel(
        IArticleRecord article,
        IArticleTextLayoutBuilder articleTextLayoutBuilder,
        ISystemClock systemClock,
        Action returnToLibrary)
        : base("开始练习")
    {
        ArgumentNullException.ThrowIfNull(article);
        ArgumentNullException.ThrowIfNull(articleTextLayoutBuilder);
        ArgumentNullException.ThrowIfNull(systemClock);
        ArgumentNullException.ThrowIfNull(returnToLibrary);

        this.returnToLibrary = returnToLibrary;

        ArticleTitle = article.Title;

        ArticleTextLayout layout = articleTextLayoutBuilder.Build(article.RawText);
        TargetText = layout.NormalizedText;
        session = new TypingSession(layout);
        inputTranslator = new WindowMessageInputTranslator(systemClock);
        statusMessage = "已进入练习页，直接开始输入即可。Esc 重练，方向键暂时只显示提示。";

        RestartCommand = new RelayCommand(Restart);
        ReturnToLibraryCommand = new RelayCommand(() => this.returnToLibrary());

        RefreshSessionState();
    }

    public string ArticleTitle { get; }

    public string TargetText { get; }

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

    public string ProgressText => $"{CurrentTextIndex} / {TargetText.Length}";

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetProperty(ref statusMessage, value);
    }

    public IRelayCommand RestartCommand { get; }

    public IRelayCommand ReturnToLibraryCommand { get; }

    public bool HandleWindowMessage(int message, nint wParam)
    {
        IKeyInputEvent? inputEvent = inputTranslator.TranslateWindowMessage(message, wParam);
        return inputEvent is not null && HandleNormalizedInput(inputEvent);
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

        session.ProcessInput(inputEvent);
        RefreshSessionState();

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
                : inputEvent.IsFromIme
                    ? "已接收输入法上屏内容。"
                    : "已接收当前输入。";
        }

        return inputEvent.Key != KeyInputKey.Unknown || !string.IsNullOrEmpty(inputEvent.ImeCommitText);
    }

    private void Restart()
    {
        session.Reset();
        inputTranslator.Reset();
        RefreshSessionState();
        StatusMessage = "已重新开始当前文章。";
    }

    private void RefreshSessionState()
    {
        ITypingSessionSnapshot snapshot = session.Snapshot;
        SessionState = snapshot.State;
        CurrentTextIndex = snapshot.CurrentTextIndex;
        CorrectCharacterCount = snapshot.CorrectCharacterCount;
        ErrorCharacterCount = snapshot.ErrorCharacterCount;
        CommittedText = new string(snapshot.Characters
            .Take(CurrentTextIndex)
            .Select(character => character.InputChar ?? '\0')
            .Where(character => character != '\0')
            .ToArray());
    }
}