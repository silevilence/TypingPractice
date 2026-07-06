using TypingCore.Abstractions;
using TypingCore.Engine;
using TypingCore.Models;
using TypingCore.Parsing;
using System.Diagnostics;

namespace TypingCore.Tests.Engine;

public class TypingSessionTests
{
    private static readonly ArticleTextLayoutBuilder LayoutBuilder = new();

    [Fact]
    public void ProcessInput_advances_session_when_commit_text_matches_target()
    {
        DateTimeOffset startedAt = new(2026, 7, 3, 10, 0, 0, TimeSpan.Zero);
        ITypingSession session = new TypingSession(LayoutBuilder.Build("ab"));

        session.ProcessInput(new KeyInputEvent(KeyInputKey.Character, startedAt, false, null, false));
        session.ProcessInput(new KeyInputEvent(KeyInputKey.Character, startedAt.AddMilliseconds(5), false, "a", false));
        session.ProcessInput(new KeyInputEvent(KeyInputKey.Character, startedAt.AddSeconds(1), false, null, false));
        session.ProcessInput(new KeyInputEvent(KeyInputKey.Character, startedAt.AddSeconds(1).AddMilliseconds(5), false, "b", false));

        ITypingSessionSnapshot snapshot = session.Snapshot;

        Assert.Equal(TypingSessionState.Completed, snapshot.State);
        Assert.Equal(2, snapshot.CurrentTextIndex);
        Assert.Equal(2, snapshot.CorrectCharacterCount);
        Assert.Equal(0, snapshot.ErrorCharacterCount);
        Assert.Collection(
            snapshot.Characters,
            first =>
            {
                Assert.Equal(0, first.TextIndex);
                Assert.Equal('a', first.TargetChar);
                Assert.Equal('a', first.InputChar);
                Assert.Equal(TypingCharacterState.Correct, first.State);
            },
            second =>
            {
                Assert.Equal(1, second.TextIndex);
                Assert.Equal('b', second.TargetChar);
                Assert.Equal('b', second.InputChar);
                Assert.Equal(TypingCharacterState.Correct, second.State);
            });

        Assert.Equal(1d, session.StatisticsProvider.Current.AverageCodeLength);
    }

    [Fact]
    public void ProcessInput_allows_backspace_to_remove_committed_error_and_retype()
    {
        DateTimeOffset startedAt = new(2026, 7, 3, 10, 30, 0, TimeSpan.Zero);
        ITypingSession session = new TypingSession(LayoutBuilder.Build("ab"));

        session.ProcessInput(new KeyInputEvent(KeyInputKey.Character, startedAt, false, null, false));
        session.ProcessInput(new KeyInputEvent(KeyInputKey.Character, startedAt.AddMilliseconds(5), false, "a", false));
        session.ProcessInput(new KeyInputEvent(KeyInputKey.Character, startedAt.AddSeconds(1), false, null, false));
        session.ProcessInput(new KeyInputEvent(KeyInputKey.Character, startedAt.AddSeconds(1).AddMilliseconds(5), false, "x", false));

        Assert.Equal(TypingSessionState.Completed, session.Snapshot.State);
        Assert.Equal(1, session.Snapshot.ErrorCharacterCount);
        Assert.Equal(TypingCharacterState.Incorrect, session.Snapshot.Characters[1].State);
        Assert.Equal('x', session.Snapshot.Characters[1].InputChar);

        session.ProcessInput(new KeyInputEvent(KeyInputKey.Backspace, startedAt.AddSeconds(2), false, null, true));

        ITypingSessionSnapshot afterBackspace = session.Snapshot;

        Assert.Equal(TypingSessionState.Running, afterBackspace.State);
        Assert.Equal(1, afterBackspace.CurrentTextIndex);
        Assert.Equal(0, afterBackspace.ErrorCharacterCount);
        Assert.Equal(TypingCharacterState.Current, afterBackspace.Characters[1].State);
        Assert.Null(afterBackspace.Characters[1].InputChar);
        Assert.Equal(1, session.StatisticsProvider.Current.BackspaceCount);

        session.ProcessInput(new KeyInputEvent(KeyInputKey.Character, startedAt.AddSeconds(3), false, null, false));
        session.ProcessInput(new KeyInputEvent(KeyInputKey.Character, startedAt.AddSeconds(3).AddMilliseconds(5), false, "b", false));

        ITypingSessionSnapshot completed = session.Snapshot;

        Assert.Equal(TypingSessionState.Completed, completed.State);
        Assert.Equal(2, completed.CorrectCharacterCount);
        Assert.Equal(0, completed.ErrorCharacterCount);
        Assert.Equal(TypingCharacterState.Correct, completed.Characters[1].State);
        Assert.Equal('b', completed.Characters[1].InputChar);

        IStatisticsSnapshot statistics = session.StatisticsProvider.Current;

        Assert.Equal(2d, statistics.AverageCodeLength);
        Assert.Equal(1, statistics.BackspaceCount);
        Assert.Equal(0.25d, statistics.BackspaceRate);
        Assert.Equal(0d, statistics.ErrorRate);
    }

    [Fact]
    public void ProcessInput_treats_backspace_inside_ime_composition_as_non_committed_edit()
    {
        DateTimeOffset startedAt = new(2026, 7, 3, 11, 0, 0, TimeSpan.Zero);
        ITypingSession session = new TypingSession(LayoutBuilder.Build("你"));

        session.ProcessInput(new KeyInputEvent(KeyInputKey.Character, startedAt, false, null, false));
        session.ProcessInput(new KeyInputEvent(KeyInputKey.Character, startedAt.AddMilliseconds(50), false, null, false));
        session.ProcessInput(new KeyInputEvent(KeyInputKey.Backspace, startedAt.AddMilliseconds(100), false, null, true));

        ITypingSessionSnapshot duringComposition = session.Snapshot;

        Assert.Equal(TypingSessionState.Running, duringComposition.State);
        Assert.Equal(0, duringComposition.CurrentTextIndex);
        Assert.Equal(0, duringComposition.CorrectCharacterCount);
        Assert.Equal(0, duringComposition.ErrorCharacterCount);
        Assert.Equal(TypingCharacterState.Current, duringComposition.Characters[0].State);
        Assert.Equal(0, session.StatisticsProvider.Current.BackspaceCount);

        session.ProcessInput(new KeyInputEvent(KeyInputKey.Character, startedAt.AddMilliseconds(150), false, null, false));
        session.ProcessInput(new KeyInputEvent(KeyInputKey.Character, startedAt.AddMilliseconds(200), true, "你", false));

        ITypingSessionSnapshot completed = session.Snapshot;

        Assert.Equal(TypingSessionState.Completed, completed.State);
        Assert.Equal(1, completed.CorrectCharacterCount);
        Assert.Equal(0, completed.ErrorCharacterCount);
        Assert.Equal(TypingCharacterState.Correct, completed.Characters[0].State);
        Assert.Equal('你', completed.Characters[0].InputChar);
        Assert.Equal(0, session.StatisticsProvider.Current.BackspaceCount);
    }

    [Fact]
    public void StatisticsProvider_uses_sliding_window_while_running_and_total_summary_after_completion()
    {
        DateTimeOffset startedAt = new(2026, 7, 3, 11, 30, 0, TimeSpan.Zero);
        ITypingSession session = new TypingSession(LayoutBuilder.Build("abc"));

        session.ProcessInput(new KeyInputEvent(KeyInputKey.Character, startedAt, false, null, false));
        session.ProcessInput(new KeyInputEvent(KeyInputKey.Character, startedAt.AddMilliseconds(5), false, "a", false));
        session.ProcessInput(new KeyInputEvent(KeyInputKey.Character, startedAt.AddSeconds(2), false, null, false));
        session.ProcessInput(new KeyInputEvent(KeyInputKey.Character, startedAt.AddSeconds(2).AddMilliseconds(5), false, "b", false));
        session.ProcessInput(new KeyInputEvent(KeyInputKey.Character, startedAt.AddSeconds(12), false, null, false));

        IStatisticsSnapshot running = session.StatisticsProvider.Current;

        Assert.Equal(TypingSessionState.Running, session.Snapshot.State);
        Assert.Equal(12d, running.KeystrokesPerMinute, 3);
        Assert.Equal(6d, running.CharactersPerMinute, 3);
        Assert.Equal(1.2d, running.WordsPerMinute, 3);
        Assert.Equal(1.5d, running.AverageCodeLength, 3);

        session.ProcessInput(new KeyInputEvent(KeyInputKey.Character, startedAt.AddSeconds(12).AddMilliseconds(5), false, "c", false));

        IStatisticsSnapshot completed = session.StatisticsProvider.Current;
        double expectedSummaryRate = 3d / TimeSpan.FromSeconds(12.005).TotalMinutes;

        Assert.Equal(TypingSessionState.Completed, session.Snapshot.State);
        Assert.Equal(expectedSummaryRate, completed.KeystrokesPerMinute, 3);
        Assert.Equal(expectedSummaryRate, completed.CharactersPerMinute, 3);
        Assert.Equal(expectedSummaryRate / 5d, completed.WordsPerMinute, 3);
        Assert.Equal(1d, completed.AverageCodeLength);
        Assert.Equal(TimeSpan.FromSeconds(12.005), completed.Elapsed);
    }

    [Fact]
    public void Pause_and_resume_exclude_paused_time_and_ignore_input_while_paused()
    {
        DateTimeOffset startedAt = new(2026, 7, 6, 10, 0, 0, TimeSpan.Zero);
        ITypingSession session = new TypingSession(LayoutBuilder.Build("ab"));

        session.ProcessInput(new KeyInputEvent(
            KeyInputKey.Character,
            startedAt,
            false,
            "a",
            false));
        session.Pause(startedAt.AddSeconds(1));
        session.ProcessInput(new KeyInputEvent(
            KeyInputKey.Character,
            startedAt.AddMinutes(5),
            false,
            "b",
            false));

        Assert.Equal(TypingSessionState.Paused, session.Snapshot.State);
        Assert.Equal(1, session.Snapshot.CurrentTextIndex);

        session.Resume(startedAt.AddMinutes(5));
        session.ProcessInput(new KeyInputEvent(
            KeyInputKey.Character,
            startedAt.AddMinutes(5).AddSeconds(1),
            false,
            "b",
            false));

        Assert.Equal(TypingSessionState.Completed, session.Snapshot.State);
        Assert.Equal(TimeSpan.FromSeconds(2), session.StatisticsProvider.Current.Elapsed);
    }

    [Fact]
    public void ProcessInput_handles_high_frequency_input_for_long_article()
    {
        const int characterCount = 5_000;
        DateTimeOffset startedAt = new(2026, 7, 6, 11, 0, 0, TimeSpan.Zero);
        ITypingSession session = new TypingSession(LayoutBuilder.Build(new string('a', characterCount)));
        Stopwatch stopwatch = Stopwatch.StartNew();

        for (int index = 0; index < characterCount; index++)
        {
            session.ProcessInput(new KeyInputEvent(
                KeyInputKey.Character,
                startedAt.AddMilliseconds(index),
                false,
                "a",
                false));
        }

        stopwatch.Stop();
        Assert.Equal(TypingSessionState.Completed, session.Snapshot.State);
        Assert.Equal(characterCount, session.Snapshot.CorrectCharacterCount);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(5),
            $"长文章高频输入耗时 {stopwatch.Elapsed.TotalSeconds:0.00} 秒。");
    }
}
