using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using TypingCore.Abstractions;
using TypingCore.Models;
using TypingCore.Parsing;
using TypingCore.Wpf.Services;
using TypingCore.Wpf.ViewModels;
using TypingCore.Wpf.Views;

namespace TypingCore.Tests.Wpf;

public sealed class PhaseTenFollowingRenderTests
{
    [Fact]
    public void FollowingTypingSegmentBuilder_groups_target_text_by_all_four_states()
    {
        IReadOnlyList<TypingCharacterSnapshot> characters =
        [
            new(0, '已', '已', TypingCharacterState.Correct),
            new(1, '过', '过', TypingCharacterState.Correct),
            new(2, '当', null, TypingCharacterState.Current),
            new(3, '错', '措', TypingCharacterState.Incorrect),
            new(4, '未', null, TypingCharacterState.Pending),
            new(5, '到', null, TypingCharacterState.Pending),
        ];

        IReadOnlyList<FollowingTypingSegment> segments = FollowingTypingSegmentBuilder.Build(characters);

        Assert.Collection(
            segments,
            segment =>
            {
                Assert.Equal("已过", segment.Text);
                Assert.Equal(TypingCharacterState.Correct, segment.State);
            },
            segment =>
            {
                Assert.Equal("当", segment.Text);
                Assert.Equal(TypingCharacterState.Current, segment.State);
            },
            segment =>
            {
                Assert.Equal("错", segment.Text);
                Assert.Equal(TypingCharacterState.Incorrect, segment.State);
            },
            segment =>
            {
                Assert.Equal("未到", segment.Text);
                Assert.Equal(TypingCharacterState.Pending, segment.State);
            });
    }

    [Fact]
    public void TypingPracticeViewModel_switches_layouts_without_resetting_shared_session()
    {
        MutableSystemClock clock = new(new DateTimeOffset(2026, 7, 6, 10, 0, 0, TimeSpan.Zero));
        TypingPracticeViewModel viewModel = new(
            new Article(
                "article-1",
                "练习文章",
                "ab",
                clock.UtcNow,
                Array.Empty<string>()),
            new ArticleTextLayoutBuilder(),
            clock,
            () => { });

        viewModel.HandleTextInput("a");
        viewModel.SelectFollowingLayoutCommand.Execute(null);

        Assert.True(viewModel.IsFollowingLayout);
        Assert.Equal("a", viewModel.CommittedText);
        Assert.Equal(1, viewModel.CurrentTextIndex);

        clock.Advance(TimeSpan.FromMilliseconds(5));
        viewModel.HandleTextInput("b");
        viewModel.SelectInterleavedLayoutCommand.Execute(null);

        Assert.True(viewModel.IsInterleavedLayout);
        Assert.Equal("ab", viewModel.CommittedText);
        Assert.Equal(2, viewModel.CurrentTextIndex);
        Assert.True(viewModel.IsCompleted);
    }

    [Fact]
    public void TypingPracticeView_layoutB_allocates_useful_height_to_both_practice_panes()
    {
        Exception? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                MutableSystemClock clock = new(new DateTimeOffset(2026, 7, 6, 10, 0, 0, TimeSpan.Zero));
                TypingPracticeViewModel viewModel = new(
                    new Article(
                        "article-1",
                        "练习文章",
                        string.Join('\n', Enumerable.Repeat("天地玄黄宇宙洪荒", 80)),
                        clock.UtcNow,
                        Array.Empty<string>()),
                    new ArticleTextLayoutBuilder(),
                    clock,
                    () => { });
                viewModel.SelectFollowingLayoutCommand.Execute(null);

                TypingPracticeView view = new()
                {
                    DataContext = viewModel,
                };

                view.Measure(new Size(1200d, 800d));
                view.Arrange(new Rect(0d, 0d, 1200d, 800d));
                view.UpdateLayout();

                ScrollViewer target = Assert.IsType<ScrollViewer>(view.FindName("FollowingTargetScrollViewer"));
                ScrollViewer input = Assert.IsType<ScrollViewer>(view.FindName("FollowingInputScrollViewer"));

                Assert.True(target.ActualHeight >= 180d, $"原文区域高度仅 {target.ActualHeight:0.#}。");
                Assert.True(input.ActualHeight >= 180d, $"输入区域高度仅 {input.ActualHeight:0.#}。");
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private sealed class MutableSystemClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; private set; } = utcNow;

        public void Advance(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
    }
}
