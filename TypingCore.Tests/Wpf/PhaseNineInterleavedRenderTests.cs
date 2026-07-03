using System.Windows;

using TypingCore.Abstractions;
using TypingCore.Models;
using TypingCore.Parsing;
using TypingCore.Wpf.Services;
using TypingCore.Wpf.ViewModels;
using TypingCore.Wpf.Views;

namespace TypingCore.Tests.Wpf;

public sealed class PhaseNineInterleavedRenderTests
{
    [Fact]
    public void InterleavedTypingRenderLayoutBuilder_wraps_mixed_width_characters_into_normalized_cells()
    {
        ArticleTextLayout layout = new ArticleTextLayoutBuilder().Build("中AB");

        InterleavedTypingRenderLayout renderLayout = InterleavedTypingRenderLayoutBuilder.Build(
            layout,
            availableWidth: 35d,
            halfCellWidth: 10d,
            rowHeight: 24d);

        Assert.Equal(2, renderLayout.Lines.Count);
        Assert.Collection(
            renderLayout.Lines[0].Cells,
            cell =>
            {
                Assert.Equal(0, cell.TextIndex);
                Assert.Equal(2, cell.WidthUnits);
                Assert.Equal(0d, cell.TargetBounds.X);
                Assert.Equal(20d, cell.TargetBounds.Width);
            },
            cell =>
            {
                Assert.Equal(1, cell.TextIndex);
                Assert.Equal(1, cell.WidthUnits);
                Assert.Equal(20d, cell.TargetBounds.X);
                Assert.Equal(10d, cell.TargetBounds.Width);
            });

        InterleavedTypingRenderCell wrappedCell = Assert.Single(renderLayout.Lines[1].Cells);
        Assert.Equal(2, wrappedCell.TextIndex);
        Assert.Equal(0d, wrappedCell.TargetBounds.X);
        Assert.Equal(10d, wrappedCell.TargetBounds.Width);
    }

    [Fact]
    public void InterleavedTypingRenderLayoutBuilder_expands_line_bounds_to_available_width()
    {
        ArticleTextLayout layout = new ArticleTextLayoutBuilder().Build("中A");

        InterleavedTypingRenderLayout renderLayout = InterleavedTypingRenderLayoutBuilder.Build(
            layout,
            availableWidth: 120d,
            halfCellWidth: 10d,
            rowHeight: 24d);

        InterleavedTypingRenderLine line = Assert.Single(renderLayout.Lines);
        Assert.Equal(120d, line.Bounds.Width);
        Assert.Equal(120d, renderLayout.Extent.Width);
    }

    [Fact]
    public void InterleavedTypingRenderLayoutBuilder_uses_line_breaks_as_layout_boundaries_without_visible_cells()
    {
        ArticleTextLayout layout = new ArticleTextLayoutBuilder().Build("甲\n乙");

        InterleavedTypingRenderLayout renderLayout = InterleavedTypingRenderLayoutBuilder.Build(
            layout,
            availableWidth: 120d,
            halfCellWidth: 10d,
            rowHeight: 24d);

        Assert.Equal(2, renderLayout.Lines.Count);
        Assert.Collection(
            renderLayout.Lines[0].Cells,
            cell => Assert.Equal(0, cell.TextIndex));
        Assert.Collection(
            renderLayout.Lines[1].Cells,
            cell => Assert.Equal(2, cell.TextIndex));
        Assert.Equal(0, renderLayout.CharacterLineMap[1]);
    }

    [Fact]
    public void InterleavedTypingRenderLayoutBuilder_limits_dirty_redraw_to_changed_lines()
    {
        ArticleTextLayout layout = new ArticleTextLayoutBuilder().Build("AB\nCD");
        InterleavedTypingRenderLayout renderLayout = InterleavedTypingRenderLayoutBuilder.Build(
            layout,
            availableWidth: 80d,
            halfCellWidth: 10d,
            rowHeight: 24d);

        IReadOnlyList<TypingCharacterSnapshot> previousCharacters =
        [
            new(0, 'A', null, TypingCharacterState.Current),
            new(1, 'B', null, TypingCharacterState.Pending),
            new(2, '\n', null, TypingCharacterState.Pending),
            new(3, 'C', null, TypingCharacterState.Pending),
            new(4, 'D', null, TypingCharacterState.Pending),
        ];

        IReadOnlyList<TypingCharacterSnapshot> currentCharacters =
        [
            new(0, 'A', 'A', TypingCharacterState.Correct),
            new(1, 'B', null, TypingCharacterState.Current),
            new(2, '\n', null, TypingCharacterState.Pending),
            new(3, 'C', null, TypingCharacterState.Pending),
            new(4, 'D', null, TypingCharacterState.Pending),
        ];

        int dirtyLineIndex = Assert.Single(
            InterleavedTypingRenderLayoutBuilder.GetDirtyLineIndices(
                renderLayout,
                previousCharacters,
                currentCharacters));

        Assert.Equal(0, dirtyLineIndex);
    }

    [Fact]
    public void TypingPracticeViewModel_exposes_layout_and_character_snapshots_for_render_surface()
    {
        MutableSystemClock clock = new(new DateTimeOffset(2026, 7, 3, 14, 0, 0, TimeSpan.Zero));
        TypingPracticeViewModel viewModel = new(
            new Article(
                "article-1",
                "练习文章",
                "中A",
                clock.UtcNow,
                Array.Empty<string>()),
            new ArticleTextLayoutBuilder(),
            clock,
            () => { });

        Assert.Equal("中A", viewModel.ArticleLayout.NormalizedText);
        Assert.Collection(
            viewModel.CharacterSnapshots,
            character => Assert.Equal(TypingCharacterState.Current, character.State),
            character => Assert.Equal(TypingCharacterState.Pending, character.State));

        bool handled = viewModel.HandleTextInput("中");

        Assert.True(handled);
        Assert.Equal(CharacterWidthKind.FullWidth, viewModel.ArticleLayout.Characters[0].WidthKind);
        Assert.Equal(CharacterWidthKind.HalfWidth, viewModel.ArticleLayout.Characters[1].WidthKind);
        Assert.Collection(
            viewModel.CharacterSnapshots,
            character =>
            {
                Assert.Equal(TypingCharacterState.Correct, character.State);
                Assert.Equal('中', character.InputChar);
            },
            character => Assert.Equal(TypingCharacterState.Current, character.State));
    }

    [Fact]
    public void InterleavedTypingRenderGeometry_keeps_input_slot_visible_for_half_width_cells()
    {
        Rect inputBounds = new(0d, 0d, 14d, 24d);

        Rect slotBounds = InterleavedTypingRenderGeometry.GetInputSlotBounds(inputBounds);

        Assert.True(slotBounds.Width >= 6d);
        Assert.True(slotBounds.Height >= 12d);
        Assert.True(slotBounds.Left > inputBounds.Left);
        Assert.True(slotBounds.Right < inputBounds.Right);
    }

    [Fact]
    public void InterleavedTypingRenderGeometry_places_caret_inside_current_input_line()
    {
        Rect inputBounds = new(40d, 60d, 18d, 28d);

        Rect caretBounds = InterleavedTypingRenderGeometry.GetCaretBounds(inputBounds);

        Assert.True(caretBounds.Width <= 3d);
        Assert.True(caretBounds.Height >= 16d);
        Assert.True(caretBounds.Left >= inputBounds.Left + 2d);
        Assert.True(caretBounds.Right <= inputBounds.Right - 2d);
    }

    [Fact]
    public void InterleavedTypingRenderGeometry_places_line_break_cue_near_used_content_end()
    {
        Rect lineBounds = new(0d, 0d, 120d, 60d);

        Rect cueBounds = InterleavedTypingRenderGeometry.GetTrailingBreakCueBounds(lineBounds, usedContentRight: 72d);

        Assert.True(cueBounds.Left >= 72d);
        Assert.True(cueBounds.Right < lineBounds.Right - 20d);
    }

    [Fact]
    public void InterleavedTypingRenderGeometry_prefers_viewport_width_over_scroll_container_width()
    {
        double layoutWidth = InterleavedTypingRenderGeometry.GetLayoutWidth(
            availableWidth: 780d,
            viewportWidth: 742d);

        Assert.Equal(742d, layoutWidth);
    }

    [Fact]
    public void InterleavedTypingRenderGeometry_scrolls_down_when_current_line_falls_below_viewport()
    {
        Rect lineBounds = new(0d, 180d, 600d, 52d);

        double targetOffset = InterleavedTypingRenderGeometry.GetScrollOffsetForLine(
            lineBounds,
            currentOffset: 0d,
            viewportHeight: 160d,
            padding: 24d);

        Assert.Equal(156d, targetOffset);
    }

    [Fact]
    public void InterleavedTypingRenderGeometry_keeps_offset_when_current_line_is_already_visible()
    {
        Rect lineBounds = new(0d, 90d, 600d, 52d);

        double targetOffset = InterleavedTypingRenderGeometry.GetScrollOffsetForLine(
            lineBounds,
            currentOffset: 40d,
            viewportHeight: 180d,
            padding: 24d);

        Assert.Equal(40d, targetOffset);
    }

    private sealed class MutableSystemClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; private set; } = utcNow;

        public void Advance(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
    }
}