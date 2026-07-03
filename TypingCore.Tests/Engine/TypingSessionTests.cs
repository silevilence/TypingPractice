using TypingCore.Abstractions;
using TypingCore.Engine;
using TypingCore.Models;
using TypingCore.Parsing;

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
}