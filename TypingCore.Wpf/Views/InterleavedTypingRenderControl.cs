using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using TypingCore.Abstractions;
using TypingCore.Models;

namespace TypingCore.Wpf.Views;

/// <summary>
/// Draws the stage-nine interleaved typing layout with aligned target and input rows.
/// </summary>
public sealed class InterleavedTypingRenderControl : FrameworkElement
{
    public static readonly DependencyProperty FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(
        typeof(InterleavedTypingRenderControl),
        new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(
        typeof(InterleavedTypingRenderControl),
        new FrameworkPropertyMetadata(SystemFonts.MessageFontSize, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty FontStretchProperty = TextElement.FontStretchProperty.AddOwner(
        typeof(InterleavedTypingRenderControl),
        new FrameworkPropertyMetadata(FontStretches.Normal, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty FontStyleProperty = TextElement.FontStyleProperty.AddOwner(
        typeof(InterleavedTypingRenderControl),
        new FrameworkPropertyMetadata(FontStyles.Normal, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty FontWeightProperty = TextElement.FontWeightProperty.AddOwner(
        typeof(InterleavedTypingRenderControl),
        new FrameworkPropertyMetadata(FontWeights.Normal, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty ArticleLayoutProperty = DependencyProperty.Register(
        nameof(ArticleLayout),
        typeof(ArticleTextLayout),
        typeof(InterleavedTypingRenderControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnArticleLayoutChanged));

    public static readonly DependencyProperty CharacterSnapshotsProperty = DependencyProperty.Register(
        nameof(CharacterSnapshots),
        typeof(IReadOnlyList<TypingCharacterSnapshot>),
        typeof(InterleavedTypingRenderControl),
        new FrameworkPropertyMetadata(null, OnCharacterSnapshotsChanged));

    public static readonly DependencyProperty CurrentTextIndexProperty = DependencyProperty.Register(
        nameof(CurrentTextIndex),
        typeof(int),
        typeof(InterleavedTypingRenderControl),
        new FrameworkPropertyMetadata(0, OnCurrentTextIndexChanged));

    private static readonly SolidColorBrush SurfaceBrush = CreateBrush(0xFA, 0xF8, 0xF5);
    private static readonly SolidColorBrush SlotBrush = CreateBrush(0xFF, 0xFF, 0xFF);
    private static readonly SolidColorBrush PrimaryTextBrush = CreateBrush(0x2D, 0x29, 0x26);
    private static readonly SolidColorBrush SecondaryTextBrush = CreateBrush(0x7A, 0x70, 0x67);
    private static readonly SolidColorBrush AccentBrush = CreateBrush(0x8B, 0x5E, 0x3C);
    private static readonly SolidColorBrush AccentSoftBrush = CreateBrush(0xF0, 0xE6, 0xD6);
    private static readonly SolidColorBrush ErrorSoftBrush = CreateBrush(0xFA, 0xE5, 0xE3);
    private static readonly SolidColorBrush BorderBrush = CreateBrush(0xE8, 0xE2, 0xDA);
    private static readonly SolidColorBrush ErrorBrush = CreateBrush(0xC7, 0x51, 0x46);
    private static readonly SolidColorBrush SuccessBrush = CreateBrush(0x5B, 0x8C, 0x5A);
    private static readonly Pen BorderPen = CreatePen(BorderBrush, 1d);
    private static readonly Pen AccentPen = CreatePen(AccentBrush, 1.5d);
    private static readonly Pen ErrorPen = CreatePen(ErrorBrush, 1.5d);
    private static readonly Pen SuccessPen = CreatePen(SuccessBrush, 1.5d);
    private static readonly Brush TransparentBrush = Brushes.Transparent;

    private readonly VisualCollection visuals;
    private readonly List<DrawingVisual> lineVisuals = [];
    private InterleavedTypingRenderLayout? renderLayout;
    private bool layoutDirty = true;
    private bool requiresFullRedraw = true;
    private IReadOnlyList<TypingCharacterSnapshot>? pendingCurrentCharacters;
    private IReadOnlyList<TypingCharacterSnapshot>? pendingPreviousCharacters;
    private double cachedHalfCellWidth = double.NaN;
    private double cachedRenderWidth = double.NaN;
    private double cachedRowHeight = double.NaN;
    private ScrollViewer? hostScrollViewer;

    public InterleavedTypingRenderControl()
    {
        visuals = new VisualCollection(this);
        SnapsToDevicePixels = true;
        UseLayoutRounding = true;
    }

    public ArticleTextLayout? ArticleLayout
    {
        get => (ArticleTextLayout?)GetValue(ArticleLayoutProperty);
        set => SetValue(ArticleLayoutProperty, value);
    }

    public FontFamily FontFamily
    {
        get => (FontFamily)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontStretch FontStretch
    {
        get => (FontStretch)GetValue(FontStretchProperty);
        set => SetValue(FontStretchProperty, value);
    }

    public FontStyle FontStyle
    {
        get => (FontStyle)GetValue(FontStyleProperty);
        set => SetValue(FontStyleProperty, value);
    }

    public FontWeight FontWeight
    {
        get => (FontWeight)GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public IReadOnlyList<TypingCharacterSnapshot>? CharacterSnapshots
    {
        get => (IReadOnlyList<TypingCharacterSnapshot>?)GetValue(CharacterSnapshotsProperty);
        set => SetValue(CharacterSnapshotsProperty, value);
    }

    public int CurrentTextIndex
    {
        get => (int)GetValue(CurrentTextIndexProperty);
        set => SetValue(CurrentTextIndexProperty, value);
    }

    protected override int VisualChildrenCount => visuals.Count;

    protected override Visual GetVisualChild(int index) => visuals[index];

    protected override Size MeasureOverride(Size availableSize)
    {
        if (ArticleLayout is null)
        {
            return new Size(0d, 0d);
        }

        double halfCellWidth = GetHalfCellWidth();
        double rowHeight = GetRowHeight();
        double measurementWidth = GetEffectiveLayoutWidth(availableSize.Width);

        InterleavedTypingRenderLayout layout = InterleavedTypingRenderLayoutBuilder.Build(
            ArticleLayout,
            measurementWidth,
            halfCellWidth,
            rowHeight);

        double desiredWidth = double.IsInfinity(measurementWidth)
            ? layout.Extent.Width
            : measurementWidth;

        return new Size(desiredWidth, layout.Extent.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        UpdateVisualTree(GetEffectiveLayoutWidth(finalSize.Width));
        return finalSize;
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == FontFamilyProperty
            || e.Property == FontSizeProperty
            || e.Property == FontStretchProperty
            || e.Property == FontStyleProperty
            || e.Property == FontWeightProperty)
        {
            InvalidateLayout();
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        drawingContext.DrawRectangle(TransparentBrush, null, new Rect(new Point(0d, 0d), RenderSize));
        UpdateVisualTree(GetEffectiveLayoutWidth(RenderSize.Width));
    }

    private static void OnArticleLayoutChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        InterleavedTypingRenderControl control = (InterleavedTypingRenderControl)dependencyObject;
        control.pendingPreviousCharacters = null;
        control.pendingCurrentCharacters = null;
        control.hostScrollViewer = null;
        control.InvalidateLayout();
    }

    private static void OnCharacterSnapshotsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        InterleavedTypingRenderControl control = (InterleavedTypingRenderControl)dependencyObject;
        control.pendingPreviousCharacters = e.OldValue as IReadOnlyList<TypingCharacterSnapshot>;
        control.pendingCurrentCharacters = e.NewValue as IReadOnlyList<TypingCharacterSnapshot>;
        control.InvalidateVisual();
    }

    private static void OnCurrentTextIndexChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        InterleavedTypingRenderControl control = (InterleavedTypingRenderControl)dependencyObject;
        control.InvalidateVisual();
    }

    private static SolidColorBrush CreateBrush(byte red, byte green, byte blue)
    {
        SolidColorBrush brush = new(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }

    private static Pen CreatePen(Brush brush, double thickness)
    {
        Pen pen = new(brush, thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round,
        };

        pen.Freeze();
        return pen;
    }

    private static Rect Shrink(Rect bounds, double amount)
    {
        if (bounds.Width <= amount * 2d || bounds.Height <= amount * 2d)
        {
            return bounds;
        }

        return new Rect(
            bounds.Left + amount,
            bounds.Top + amount,
            bounds.Width - (amount * 2d),
            bounds.Height - (amount * 2d));
    }

    private void ClearVisuals()
    {
        while (lineVisuals.Count > 0)
        {
            DrawingVisual visual = lineVisuals[^1];
            visuals.Remove(visual);
            lineVisuals.RemoveAt(lineVisuals.Count - 1);
        }
    }

    private void DrawLine(int lineIndex)
    {
        if (renderLayout is null || lineIndex < 0 || lineIndex >= lineVisuals.Count)
        {
            return;
        }

        InterleavedTypingRenderLine line = renderLayout.Lines[lineIndex];
        DrawingVisual visual = lineVisuals[lineIndex];

        using DrawingContext drawingContext = visual.RenderOpen();

        Rect inputLineBounds = InterleavedTypingRenderGeometry.GetInputLineBounds(line.Bounds);
        drawingContext.DrawRoundedRectangle(SlotBrush, BorderPen, inputLineBounds, 10d, 10d);

        foreach (InterleavedTypingRenderCell cell in line.Cells)
        {
            DrawCell(drawingContext, cell, GetSnapshot(cell.TextIndex));
        }

        DrawTrailingLineBreakCue(drawingContext, line);
    }

    private void DrawCell(DrawingContext drawingContext, InterleavedTypingRenderCell cell, TypingCharacterSnapshot? snapshot)
    {
        if (cell.TargetChar == '\n')
        {
            return;
        }

        Rect combinedBounds = new(
            cell.TargetBounds.Left,
            cell.TargetBounds.Top,
            cell.TargetBounds.Width,
            cell.InputBounds.Bottom - cell.TargetBounds.Top);

        Brush targetBrush = PrimaryTextBrush;
        Brush inputBrush = AccentBrush;

        TypingCharacterState state = snapshot?.State ?? TypingCharacterState.Pending;
        switch (state)
        {
            case TypingCharacterState.Current:
                drawingContext.DrawRoundedRectangle(AccentSoftBrush, null, Shrink(combinedBounds, 2d), 10d, 10d);
                targetBrush = AccentBrush;
                break;
            case TypingCharacterState.Correct:
                targetBrush = SecondaryTextBrush;
                inputBrush = SuccessBrush;
                break;
            case TypingCharacterState.Incorrect:
                targetBrush = ErrorBrush;
                inputBrush = ErrorBrush;
                drawingContext.DrawRoundedRectangle(ErrorSoftBrush, null, Shrink(combinedBounds, 2d), 10d, 10d);
                break;
        }

        DrawCharacterText(drawingContext, GetDisplayText(cell.TargetChar), cell.TargetBounds, targetBrush);

        if (snapshot?.InputChar is char inputChar)
        {
            DrawCharacterText(drawingContext, GetDisplayText(inputChar), cell.InputBounds, inputBrush);
        }

        if (state == TypingCharacterState.Current)
        {
            drawingContext.DrawRoundedRectangle(AccentBrush, null, InterleavedTypingRenderGeometry.GetCaretBounds(cell.InputBounds), 1d, 1d);
        }
    }

    private void DrawTrailingLineBreakCue(DrawingContext drawingContext, InterleavedTypingRenderLine line)
    {
        if (line.TrailingLineBreakTextIndex is not int lineBreakIndex)
        {
            return;
        }

        TypingCharacterSnapshot? snapshot = GetSnapshot(lineBreakIndex);
        if (snapshot is null)
        {
            return;
        }

        double usedContentRight = line.Cells.Count == 0
            ? line.Bounds.Left
            : line.Cells[^1].InputBounds.Right;
        Rect cueBounds = InterleavedTypingRenderGeometry.GetTrailingBreakCueBounds(line.Bounds, usedContentRight);
        if (snapshot.State == TypingCharacterState.Current)
        {
            DrawCharacterText(drawingContext, "↵", cueBounds, AccentBrush);
            drawingContext.DrawRoundedRectangle(AccentBrush, null, InterleavedTypingRenderGeometry.GetCaretBounds(cueBounds), 1d, 1d);
            return;
        }

        if (snapshot.State == TypingCharacterState.Incorrect && snapshot.InputChar is char inputChar)
        {
            DrawCharacterText(drawingContext, GetDisplayText(inputChar), cueBounds, ErrorBrush);
            return;
        }

        if (snapshot.State == TypingCharacterState.Correct)
        {
            DrawCharacterText(drawingContext, "↵", cueBounds, SecondaryTextBrush);
        }
    }

    private void DrawCharacterText(DrawingContext drawingContext, string text, Rect bounds, Brush brush)
    {
        if (string.IsNullOrEmpty(text) || bounds.Width <= 0d || bounds.Height <= 0d)
        {
            return;
        }

        FormattedText formattedText = new(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
            FontSize,
            brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        Point origin = new(
            bounds.Left + Math.Max(0d, (bounds.Width - formattedText.WidthIncludingTrailingWhitespace) / 2d),
            bounds.Top + Math.Max(0d, (bounds.Height - formattedText.Height) / 2d));

        drawingContext.DrawText(formattedText, origin);
    }

    private void EnsureLineVisualCount(int count)
    {
        while (lineVisuals.Count < count)
        {
            DrawingVisual visual = new();
            lineVisuals.Add(visual);
            visuals.Add(visual);
        }

        while (lineVisuals.Count > count)
        {
            DrawingVisual visual = lineVisuals[^1];
            visuals.Remove(visual);
            lineVisuals.RemoveAt(lineVisuals.Count - 1);
        }
    }

    private TypingCharacterSnapshot? GetSnapshot(int textIndex)
    {
        IReadOnlyList<TypingCharacterSnapshot>? snapshots = CharacterSnapshots;
        return snapshots is not null && textIndex >= 0 && textIndex < snapshots.Count
            ? snapshots[textIndex]
            : null;
    }

    private static string GetDisplayText(char value)
        => value switch
        {
            '\t' => "\u21E5",
            _ => value.ToString(),
        };

    private double GetHalfCellWidth()
    {
        FormattedText sample = new(
            "0",
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
            FontSize,
            PrimaryTextBrush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        return InterleavedTypingRenderGeometry.GetHalfCellWidth(
            sample.WidthIncludingTrailingWhitespace,
            FontSize);
    }

    private double GetRowHeight() => Math.Max(28d, Math.Ceiling(FontSize * 1.7d));

    private void InvalidateLayout()
    {
        layoutDirty = true;
        requiresFullRedraw = true;
        cachedHalfCellWidth = double.NaN;
        cachedRenderWidth = double.NaN;
        cachedRowHeight = double.NaN;
        renderLayout = null;
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void RedrawDirtyLines()
    {
        if (renderLayout is null)
        {
            return;
        }

        IReadOnlyList<int> dirtyLines = InterleavedTypingRenderLayoutBuilder.GetDirtyLineIndices(
            renderLayout,
            pendingPreviousCharacters,
            pendingCurrentCharacters);

        if (dirtyLines.Count == 0)
        {
            return;
        }

        foreach (int lineIndex in dirtyLines)
        {
            DrawLine(lineIndex);
        }
    }

    private void UpdateVisualTree(double availableWidth)
    {
        if (ArticleLayout is null || !double.IsFinite(availableWidth) || availableWidth <= 0d)
        {
            ClearVisuals();
            return;
        }

        double halfCellWidth = GetHalfCellWidth();
        double rowHeight = GetRowHeight();
        bool metricsChanged = layoutDirty
            || renderLayout is null
            || !AreClose(cachedRenderWidth, availableWidth)
            || !AreClose(cachedHalfCellWidth, halfCellWidth)
            || !AreClose(cachedRowHeight, rowHeight);

        if (metricsChanged)
        {
            renderLayout = InterleavedTypingRenderLayoutBuilder.Build(
                ArticleLayout,
                availableWidth,
                halfCellWidth,
                rowHeight);
            cachedRenderWidth = availableWidth;
            cachedHalfCellWidth = halfCellWidth;
            cachedRowHeight = rowHeight;
            layoutDirty = false;
            requiresFullRedraw = true;
        }

        if (renderLayout is null)
        {
            ClearVisuals();
            return;
        }

        EnsureLineVisualCount(renderLayout.Lines.Count);

        if (requiresFullRedraw)
        {
            for (int lineIndex = 0; lineIndex < renderLayout.Lines.Count; lineIndex++)
            {
                DrawLine(lineIndex);
            }

            requiresFullRedraw = false;
            pendingPreviousCharacters = CharacterSnapshots;
            pendingCurrentCharacters = CharacterSnapshots;
            EnsureCurrentLineVisible();
            return;
        }

        RedrawDirtyLines();
        pendingPreviousCharacters = null;
        pendingCurrentCharacters = null;
        EnsureCurrentLineVisible();
    }

    private void EnsureCurrentLineVisible()
    {
        if (renderLayout is null || renderLayout.Lines.Count == 0)
        {
            return;
        }

        ScrollViewer? scrollViewer = GetHostScrollViewer();
        if (scrollViewer is null || scrollViewer.ViewportHeight <= 0d)
        {
            return;
        }

        Rect lineBounds;
        if (!TryGetLineBoundsForTextIndex(CurrentTextIndex, out lineBounds))
        {
            lineBounds = renderLayout.Lines[^1].Bounds;
        }

        double targetOffset = InterleavedTypingRenderGeometry.GetScrollOffsetForLine(
            lineBounds,
            scrollViewer.VerticalOffset,
            scrollViewer.ViewportHeight,
            padding: 24d);

        if (!AreClose(targetOffset, scrollViewer.VerticalOffset))
        {
            scrollViewer.ScrollToVerticalOffset(targetOffset);
        }
    }

    private double GetEffectiveLayoutWidth(double fallbackWidth)
    {
        ScrollViewer? scrollViewer = GetHostScrollViewer();
        double viewportWidth = scrollViewer?.ViewportWidth ?? double.NaN;
        return InterleavedTypingRenderGeometry.GetLayoutWidth(fallbackWidth, viewportWidth);
    }

    private ScrollViewer? GetHostScrollViewer()
    {
        if (hostScrollViewer is not null)
        {
            return hostScrollViewer;
        }

        DependencyObject? current = this;
        while (current is not null)
        {
            current = VisualTreeHelper.GetParent(current);
            if (current is ScrollViewer scrollViewer)
            {
                hostScrollViewer = scrollViewer;
                return hostScrollViewer;
            }
        }

        return null;
    }

    private bool TryGetLineBoundsForTextIndex(int textIndex, out Rect lineBounds)
    {
        lineBounds = default;

        if (renderLayout is null || renderLayout.Lines.Count == 0)
        {
            return false;
        }

        if (textIndex < 0)
        {
            return false;
        }

        if (renderLayout.CharacterLineMap.TryGetValue(textIndex, out int lineIndex)
            && lineIndex >= 0
            && lineIndex < renderLayout.Lines.Count)
        {
            lineBounds = renderLayout.Lines[lineIndex].Bounds;
            return true;
        }

        return false;
    }

    private static bool AreClose(double left, double right)
        => Math.Abs(left - right) < 0.5d;
}

internal static class InterleavedTypingRenderLayoutBuilder
{
    public static InterleavedTypingRenderLayout Build(
        ArticleTextLayout articleLayout,
        double availableWidth,
        double halfCellWidth,
        double rowHeight)
    {
        ArgumentNullException.ThrowIfNull(articleLayout);

        double safeHalfCellWidth = Math.Max(1d, halfCellWidth);
        double safeRowHeight = Math.Max(1d, rowHeight);
        double rowGap = Math.Ceiling(safeRowHeight * 0.35d);
        double lineGap = Math.Ceiling(safeRowHeight * 0.55d);
        double maxLineWidth = double.IsFinite(availableWidth) && availableWidth > 0d
            ? availableWidth
            : double.MaxValue;

        List<InterleavedTypingRenderLine> lines = [];
        Dictionary<int, int> characterLineMap = [];
        List<InterleavedTypingRenderCell> currentCells = [];
        int lineIndex = 0;
        double lineTop = 0d;
        double x = 0d;

        foreach (ArticleChar articleChar in articleLayout.Characters)
        {
            if (articleChar.IsLineBreak)
            {
                characterLineMap[articleChar.Index] = lineIndex;
                FinalizeCurrentLine(articleChar.Index);
                continue;
            }

            int widthUnits = articleChar.WidthKind == CharacterWidthKind.FullWidth ? 2 : 1;
            double cellWidth = safeHalfCellWidth * widthUnits;

            if (currentCells.Count > 0 && x + cellWidth > maxLineWidth)
            {
                FinalizeCurrentLine(null);
            }

            Rect targetBounds = new(x, lineTop, cellWidth, safeRowHeight);
            Rect inputBounds = new(x, lineTop + safeRowHeight + rowGap, cellWidth, safeRowHeight);
            currentCells.Add(new InterleavedTypingRenderCell(articleChar.Index, articleChar.Value, widthUnits, targetBounds, inputBounds));
            characterLineMap[articleChar.Index] = lineIndex;
            x += cellWidth;
        }

        if (currentCells.Count > 0 || lines.Count == 0)
        {
            FinalizeCurrentLine(null);
        }

        double extentWidth = lines.Count == 0 ? 0d : lines.Max(line => line.Bounds.Width);
        double extentHeight = lines.Count == 0 ? safeRowHeight * 2d + rowGap : lines[^1].Bounds.Bottom;

        return new InterleavedTypingRenderLayout(lines, characterLineMap, new Size(extentWidth, extentHeight));

        void FinalizeCurrentLine(int? trailingLineBreakTextIndex)
        {
            double lineHeight = (safeRowHeight * 2d) + rowGap;
            double usedLineWidth = currentCells.Count == 0 ? 0d : currentCells[^1].TargetBounds.Right;
            double lineWidth = double.IsFinite(maxLineWidth) && maxLineWidth != double.MaxValue
                ? Math.Max(usedLineWidth, maxLineWidth)
                : usedLineWidth;
            lines.Add(new InterleavedTypingRenderLine(
                lineIndex,
                currentCells.ToArray(),
                new Rect(0d, lineTop, lineWidth, lineHeight),
                trailingLineBreakTextIndex));

            currentCells = [];
            lineIndex++;
            lineTop += lineHeight + lineGap;
            x = 0d;
        }
    }

    public static IReadOnlyList<int> GetDirtyLineIndices(
        InterleavedTypingRenderLayout renderLayout,
        IReadOnlyList<TypingCharacterSnapshot>? previousCharacters,
        IReadOnlyList<TypingCharacterSnapshot>? currentCharacters)
    {
        ArgumentNullException.ThrowIfNull(renderLayout);

        if (currentCharacters is null)
        {
            return [];
        }

        if (previousCharacters is null)
        {
            return renderLayout.Lines.Select(line => line.LineIndex).ToArray();
        }

        int maxCount = Math.Max(previousCharacters.Count, currentCharacters.Count);
        SortedSet<int> dirtyLineIndices = [];

        for (int index = 0; index < maxCount; index++)
        {
            TypingCharacterSnapshot? previous = index < previousCharacters.Count ? previousCharacters[index] : null;
            TypingCharacterSnapshot? current = index < currentCharacters.Count ? currentCharacters[index] : null;

            if (previous?.State == current?.State && previous?.InputChar == current?.InputChar)
            {
                continue;
            }

            if (renderLayout.CharacterLineMap.TryGetValue(index, out int lineIndex))
            {
                dirtyLineIndices.Add(lineIndex);
            }
        }

        return dirtyLineIndices.ToArray();
    }
}

internal static class InterleavedTypingRenderGeometry
{
    public static double GetLayoutWidth(double availableWidth, double viewportWidth)
    {
        if (double.IsFinite(viewportWidth) && viewportWidth > 0d)
        {
            return viewportWidth;
        }

        if (double.IsFinite(availableWidth) && availableWidth > 0d)
        {
            return availableWidth;
        }

        return double.PositiveInfinity;
    }

    public static Rect GetCaretBounds(Rect inputBounds)
    {
        double height = Math.Max(16d, inputBounds.Height - 10d);
        double top = inputBounds.Top + Math.Max(3d, (inputBounds.Height - height) / 2d);
        double left = inputBounds.Left + Math.Max(2d, Math.Min(5d, inputBounds.Width * 0.2d));
        return new Rect(left, top, 2d, height);
    }

    public static double GetHalfCellWidth(double measuredGlyphWidth, double fontSize)
    {
        double readableMinimum = Math.Max(18d, fontSize * 1.15d);
        double measuredPadding = measuredGlyphWidth + (fontSize * 0.45d);
        return Math.Ceiling(Math.Max(readableMinimum, measuredPadding));
    }

    public static Rect GetInputLineBounds(Rect lineBounds)
    {
        double height = Math.Max(20d, (lineBounds.Height * 0.44d));
        double top = lineBounds.Bottom - height;
        return new Rect(lineBounds.Left, top, lineBounds.Width, height);
    }

    public static Rect GetInputSlotBounds(Rect inputBounds)
    {
        double horizontalInset = Math.Max(3d, Math.Min(6d, inputBounds.Width * 0.22d));
        double verticalInset = Math.Max(3d, Math.Min(6d, inputBounds.Height * 0.18d));

        double width = Math.Max(6d, inputBounds.Width - (horizontalInset * 2d));
        double height = Math.Max(12d, inputBounds.Height - (verticalInset * 2d));
        double left = inputBounds.Left + ((inputBounds.Width - width) / 2d);
        double top = inputBounds.Top + ((inputBounds.Height - height) / 2d);

        return new Rect(left, top, width, height);
    }

    public static Rect GetTrailingBreakCueBounds(Rect lineBounds, double usedContentRight)
    {
        Rect inputLineBounds = GetInputLineBounds(lineBounds);
        double width = Math.Max(16d, Math.Min(22d, inputLineBounds.Width * 0.08d));
        double preferredLeft = Math.Max(inputLineBounds.Left + 4d, usedContentRight + 4d);
        double left = Math.Min(preferredLeft, inputLineBounds.Right - width - 4d);
        return new Rect(left, inputLineBounds.Top, width, inputLineBounds.Height);
    }

    public static double GetScrollOffsetForLine(Rect lineBounds, double currentOffset, double viewportHeight, double padding)
    {
        if (viewportHeight <= 0d)
        {
            return currentOffset;
        }

        double safePadding = Math.Max(0d, padding);
        double maxVisiblePadding = Math.Max(0d, viewportHeight - lineBounds.Height - 4d);
        double effectivePadding = Math.Min(safePadding, maxVisiblePadding);
        double visibleTop = currentOffset + effectivePadding;
        double visibleBottom = currentOffset + viewportHeight - effectivePadding;

        if (lineBounds.Top >= visibleTop && lineBounds.Bottom <= visibleBottom)
        {
            return currentOffset;
        }

        return Math.Max(0d, lineBounds.Top - effectivePadding);
    }
}

internal sealed record InterleavedTypingRenderLayout(
    IReadOnlyList<InterleavedTypingRenderLine> Lines,
    IReadOnlyDictionary<int, int> CharacterLineMap,
    Size Extent);

internal sealed record InterleavedTypingRenderLine(
    int LineIndex,
    IReadOnlyList<InterleavedTypingRenderCell> Cells,
    Rect Bounds,
    int? TrailingLineBreakTextIndex);

internal sealed record InterleavedTypingRenderCell(
    int TextIndex,
    char TargetChar,
    int WidthUnits,
    Rect TargetBounds,
    Rect InputBounds);