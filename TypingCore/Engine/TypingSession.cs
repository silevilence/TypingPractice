using TypingCore.Abstractions;
using TypingCore.Models;

namespace TypingCore.Engine;

/// <summary>
/// Implements the core per-character typing comparison state machine.
/// </summary>
/// <remarks>
/// Instances are not thread-safe and are expected to be driven by a single UI event stream.
/// </remarks>
public sealed class TypingSession : ITypingSession
{
    private readonly ArticleTextLayout layout;
    private readonly List<CommittedCharacter> committedCharacters = new();
    private readonly SessionStatisticsProvider statisticsProvider;
    private TypingSessionState state;
    private ITypingSessionSnapshot snapshot;
    private SessionStatistics statisticsSnapshot;
    private int totalRawKeyCount;
    private int committedBackspaceCount;
    private int pendingCompositionKeyCount;
    private DateTimeOffset? firstInputTimestamp;
    private DateTimeOffset? lastInputTimestamp;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypingSession"/> class.
    /// </summary>
    /// <param name="layout">The normalized article layout to compare against.</param>
    public TypingSession(ArticleTextLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        this.layout = layout;
        statisticsProvider = new SessionStatisticsProvider(this);
        state = TypingSessionState.NotStarted;
        snapshot = BuildSnapshot();
        statisticsSnapshot = BuildStatisticsSnapshot();
    }

    /// <inheritdoc />
    public ITypingSessionSnapshot Snapshot => snapshot;

    /// <inheritdoc />
    public IStatisticsProvider StatisticsProvider => statisticsProvider;

    /// <inheritdoc />
    public void ProcessInput(IKeyInputEvent inputEvent)
    {
        ArgumentNullException.ThrowIfNull(inputEvent);

        bool hasCommitText = !string.IsNullOrEmpty(inputEvent.ImeCommitText);
        bool isBackspace = inputEvent.IsBackspace || inputEvent.Key == KeyInputKey.Backspace;
        bool isCompositionKey = IsCompositionKey(inputEvent.Key);

        if (!hasCommitText && !isBackspace && !isCompositionKey)
        {
            return;
        }

        if (state == TypingSessionState.Completed && !isBackspace)
        {
            return;
        }

        if (state == TypingSessionState.NotStarted)
        {
            state = TypingSessionState.Running;
        }

        firstInputTimestamp ??= inputEvent.Timestamp;
        lastInputTimestamp = inputEvent.Timestamp;

        if (!hasCommitText)
        {
            totalRawKeyCount++;
        }

        if (isBackspace)
        {
            ProcessBackspace();
            RefreshSnapshots();
            return;
        }

        if (hasCommitText)
        {
            CommitText(inputEvent.ImeCommitText!);
            RefreshSnapshots();
            return;
        }

        pendingCompositionKeyCount++;
        RefreshSnapshots();
    }

    /// <inheritdoc />
    public void Reset()
    {
        committedCharacters.Clear();
        totalRawKeyCount = 0;
        committedBackspaceCount = 0;
        pendingCompositionKeyCount = 0;
        firstInputTimestamp = null;
        lastInputTimestamp = null;
        state = TypingSessionState.NotStarted;
        RefreshSnapshots();
    }

    private void ProcessBackspace()
    {
        if (pendingCompositionKeyCount > 0)
        {
            pendingCompositionKeyCount--;
            return;
        }

        if (committedCharacters.Count == 0)
        {
            return;
        }

        committedCharacters.RemoveAt(committedCharacters.Count - 1);
        committedBackspaceCount++;
        state = TypingSessionState.Running;
    }

    private void CommitText(string commitText)
    {
        pendingCompositionKeyCount = 0;

        foreach (char inputChar in commitText)
        {
            if (committedCharacters.Count >= layout.NormalizedText.Length)
            {
                break;
            }

            int textIndex = committedCharacters.Count;
            char targetChar = layout.NormalizedText[textIndex];
            committedCharacters.Add(new CommittedCharacter(textIndex, inputChar, inputChar == targetChar));
        }

        if (committedCharacters.Count >= layout.NormalizedText.Length && layout.NormalizedText.Length > 0)
        {
            state = TypingSessionState.Completed;
        }
    }

    private void RefreshSnapshots()
    {
        snapshot = BuildSnapshot();
        statisticsSnapshot = BuildStatisticsSnapshot();
    }

    private TypingSessionSnapshot BuildSnapshot()
    {
        List<TypingCharacterSnapshot> characters = new(layout.NormalizedText.Length);
        int correctCount = committedCharacters.Count(character => character.IsCorrect);
        int errorCount = committedCharacters.Count - correctCount;

        for (int textIndex = 0; textIndex < layout.NormalizedText.Length; textIndex++)
        {
            if (textIndex < committedCharacters.Count)
            {
                CommittedCharacter committedCharacter = committedCharacters[textIndex];
                characters.Add(new TypingCharacterSnapshot(
                    textIndex,
                    layout.NormalizedText[textIndex],
                    committedCharacter.InputChar,
                    committedCharacter.IsCorrect ? TypingCharacterState.Correct : TypingCharacterState.Incorrect));
                continue;
            }

            TypingCharacterState characterState = textIndex == committedCharacters.Count && state != TypingSessionState.Completed
                ? TypingCharacterState.Current
                : TypingCharacterState.Pending;

            characters.Add(new TypingCharacterSnapshot(
                textIndex,
                layout.NormalizedText[textIndex],
                null,
                characterState));
        }

        return new TypingSessionSnapshot(
            state,
            committedCharacters.Count,
            correctCount,
            errorCount,
            characters);
    }

    private SessionStatistics BuildStatisticsSnapshot()
    {
        int committedCharacterCount = committedCharacters.Count;
        int errorCount = committedCharacters.Count(character => !character.IsCorrect);
        TimeSpan elapsed = GetElapsed();
        double elapsedMinutes = elapsed.TotalMinutes;
        double keystrokesPerMinute = elapsedMinutes > 0 ? totalRawKeyCount / elapsedMinutes : 0;
        double charactersPerMinute = elapsedMinutes > 0 ? committedCharacterCount / elapsedMinutes : 0;

        return new SessionStatistics(
            keystrokesPerMinute,
            charactersPerMinute,
            charactersPerMinute / 5d,
            committedCharacterCount > 0 ? (double)totalRawKeyCount / committedCharacterCount : 0,
            committedBackspaceCount,
            layout.NormalizedText.Length > 0 ? (double)errorCount / layout.NormalizedText.Length : 0,
            elapsed);
    }

    private TimeSpan GetElapsed()
    {
        if (!firstInputTimestamp.HasValue || !lastInputTimestamp.HasValue)
        {
            return TimeSpan.Zero;
        }

        TimeSpan elapsed = lastInputTimestamp.Value - firstInputTimestamp.Value;
        return elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
    }

    private static bool IsCompositionKey(KeyInputKey key)
    {
        return key is KeyInputKey.Character or KeyInputKey.Space or KeyInputKey.Enter or KeyInputKey.Tab;
    }

    private readonly record struct CommittedCharacter(int TextIndex, char InputChar, bool IsCorrect);

    private sealed class SessionStatisticsProvider : IStatisticsProvider
    {
        private readonly TypingSession session;

        public SessionStatisticsProvider(TypingSession session)
        {
            this.session = session;
        }

        public IStatisticsSnapshot Current => session.statisticsSnapshot;
    }
}