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

    private static readonly Brush PendingBrush = CreateBrush(0x2D, 0x29, 0x26);
    private static readonly Brush CurrentBrush = CreateBrush(0x8B, 0x5E, 0x3C);
    private static readonly Brush CurrentBackgroundBrush = CreateBrush(0xF0, 0xE6, 0xD6);
    private static readonly Brush CompletedBrush = CreateBrush(0x7A, 0x70, 0x67);
    private static readonly Brush ErrorBrush = CreateBrush(0xC7, 0x51, 0x46);
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

    private static SolidColorBrush CreateBrush(byte red, byte green, byte blue)
    {
        SolidColorBrush brush = new(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }

    private void RebuildInlines()
    {
        Inlines.Clear();
        currentRun = null;
        lastRun = null;

        foreach (FollowingTypingSegment segment in FollowingTypingSegmentBuilder.Build(CharacterSnapshots))
        {
            Run run = new(segment.Text)
            {
                Foreground = segment.State switch
                {
                    TypingCharacterState.Current => CurrentBrush,
                    TypingCharacterState.Correct => CompletedBrush,
                    TypingCharacterState.Incorrect => ErrorBrush,
                    _ => PendingBrush,
                },
            };

            if (segment.State == TypingCharacterState.Current)
            {
                run.Background = CurrentBackgroundBrush;
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
