using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using TypingCore.Abstractions;

namespace TypingCore.Wpf.Views;

/// <summary>
/// Renders the layout-B target text as state-colored segments and keeps the current segment visible.
/// </summary>
public sealed class FollowingTypingTextBlock : TextBlock
{
    public static readonly DependencyProperty CharacterSnapshotsProperty = DependencyProperty.Register(
        nameof(CharacterSnapshots),
        typeof(IReadOnlyList<TypingCharacterSnapshot>),
        typeof(FollowingTypingTextBlock),
        new FrameworkPropertyMetadata(null, OnCharacterSnapshotsChanged));

    private Run? currentRun;
    private Run? lastRun;

    public IReadOnlyList<TypingCharacterSnapshot>? CharacterSnapshots
    {
        get => (IReadOnlyList<TypingCharacterSnapshot>?)GetValue(CharacterSnapshotsProperty);
        set => SetValue(CharacterSnapshotsProperty, value);
    }

    private static void OnCharacterSnapshotsChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
        => ((FollowingTypingTextBlock)dependencyObject).RebuildInlines();

    private void RebuildInlines()
    {
        Inlines.Clear();
        currentRun = null;
        lastRun = null;

        foreach (FollowingTypingSegment segment in FollowingTypingSegmentBuilder.Build(CharacterSnapshots))
        {
            Run run = new(segment.Text);
            run.SetResourceReference(
                TextElement.ForegroundProperty,
                segment.State switch
                {
                    TypingCharacterState.Current => "AccentBrush",
                    TypingCharacterState.Correct => "SecondaryTextBrush",
                    TypingCharacterState.Incorrect => "ErrorBrush",
                    _ => "PrimaryTextBrush",
                });

            if (segment.State == TypingCharacterState.Current)
            {
                run.SetResourceReference(TextElement.BackgroundProperty, "TagBackgroundBrush");
                run.FontWeight = FontWeights.SemiBold;
                currentRun = run;
            }
            else if (segment.State == TypingCharacterState.Incorrect)
            {
                run.FontWeight = FontWeights.SemiBold;
                run.TextDecorations = System.Windows.TextDecorations.Underline;
            }

            Inlines.Add(run);
            lastRun = run;
        }

        if (lastRun is not null)
        {
            lastRun = new Run("\u200B");
            Inlines.Add(lastRun);
        }

        Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(() => (currentRun ?? lastRun)?.BringIntoView()));
    }
}

internal static class FollowingTypingSegmentBuilder
{
    public static IReadOnlyList<FollowingTypingSegment> Build(
        IReadOnlyList<TypingCharacterSnapshot>? characters)
    {
        if (characters is null || characters.Count == 0)
        {
            return [];
        }

        List<FollowingTypingSegment> segments = [];
        StringBuilder text = new();
        TypingCharacterState state = characters[0].State;

        foreach (TypingCharacterSnapshot character in characters)
        {
            if (character.State != state)
            {
                segments.Add(new FollowingTypingSegment(text.ToString(), state));
                text.Clear();
                state = character.State;
            }

            text.Append(character.TargetChar);
        }

        segments.Add(new FollowingTypingSegment(text.ToString(), state));
        return segments;
    }
}

internal sealed record FollowingTypingSegment(string Text, TypingCharacterState State);
