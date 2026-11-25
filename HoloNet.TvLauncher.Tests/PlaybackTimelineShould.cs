using HoloNet.TvLauncher.Models;
using HoloNet.TvLauncher.Views;

namespace HoloNet.TvLauncher.Tests;

/// <summary>
/// Verifies the playback controls' timeline maths: reading the live position from a player's
/// last report, D-pad skipping (see <see cref="PlaybackScrubber"/>), and how times are printed.
/// </summary>
public class PlaybackTimelineShould
{
    private static readonly DateTimeOffset ReportedAt = new(2025, 1, 1, 20, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Episode = TimeSpan.FromMinutes(55);
    private static readonly TimeSpan Step = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SettleTimeout = TimeSpan.FromSeconds(3);

    private static PlaybackSnapshot Snapshot(TimeSpan position, bool isPlaying = true, DateTimeOffset? updatedAt = null) =>
        new(isPlaying, position, Episode, updatedAt ?? ReportedAt);

    [Fact]
    public void AdvanceThePosition_WhilePlaying()
    {
        var snapshot = Snapshot(TimeSpan.FromMinutes(10));

        Assert.Equal(TimeSpan.FromSeconds(605), snapshot.PositionAt(ReportedAt.AddSeconds(5)));
    }

    [Fact]
    public void HoldThePosition_WhilePaused()
    {
        var snapshot = Snapshot(TimeSpan.FromMinutes(10), isPlaying: false);

        Assert.Equal(TimeSpan.FromMinutes(10), snapshot.PositionAt(ReportedAt.AddMinutes(1)));
    }

    [Fact]
    public void StopAtTheEnd_WhenExtrapolatingPastIt()
    {
        var snapshot = Snapshot(Episode - TimeSpan.FromSeconds(2));

        Assert.Equal(Episode, snapshot.PositionAt(ReportedAt.AddMinutes(1)));
    }

    [Fact]
    public void AddUpPresses_IntoOnePendingTarget()
    {
        var scrubber = new PlaybackScrubber(Step, SettleTimeout);
        var snapshot = Snapshot(TimeSpan.FromMinutes(10), isPlaying: false);

        scrubber.Nudge(1, snapshot, ReportedAt);
        scrubber.Nudge(1, snapshot, ReportedAt);
        scrubber.Nudge(-1, snapshot, ReportedAt);
        scrubber.Nudge(1, snapshot, ReportedAt);

        Assert.True(scrubber.IsScrubbing);
        Assert.Equal(TimeSpan.FromSeconds(620), scrubber.PositionAt(snapshot, ReportedAt));
        Assert.Equal(TimeSpan.FromSeconds(620), scrubber.Commit(ReportedAt));
    }

    [Theory]
    [InlineData(5, -1, 0)]
    [InlineData(3295, 1, 3300)]
    public void KeepTheTarget_WithinTheTitle(int fromSeconds, int steps, int expectedSeconds)
    {
        var scrubber = new PlaybackScrubber(Step, SettleTimeout);

        var target = scrubber.Nudge(steps, Snapshot(TimeSpan.FromSeconds(fromSeconds), isPlaying: false), ReportedAt);

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), target);
    }

    [Fact]
    public void ShowTheTarget_UntilThePlayerReportsTheSeek()
    {
        var scrubber = new PlaybackScrubber(Step, SettleTimeout);
        var beforeSeek = Snapshot(TimeSpan.FromMinutes(10), isPlaying: false);
        scrubber.Nudge(3, beforeSeek, ReportedAt);
        var committedAt = ReportedAt.AddSeconds(1);
        scrubber.Commit(committedAt);

        Assert.False(scrubber.IsScrubbing);
        Assert.Equal(TimeSpan.FromSeconds(630), scrubber.PositionAt(beforeSeek, committedAt.AddSeconds(1)));

        var afterSeek = Snapshot(TimeSpan.FromSeconds(629), isPlaying: false, updatedAt: committedAt.AddSeconds(1));
        Assert.Equal(TimeSpan.FromSeconds(629), scrubber.PositionAt(afterSeek, committedAt.AddSeconds(1)));
    }

    [Fact]
    public void FallBackToThePlayersPosition_WhenTheSeekIsNeverReported()
    {
        var scrubber = new PlaybackScrubber(Step, SettleTimeout);
        var snapshot = Snapshot(TimeSpan.FromMinutes(10), isPlaying: false);
        scrubber.Nudge(3, snapshot, ReportedAt);
        scrubber.Commit(ReportedAt);

        Assert.Equal(TimeSpan.FromMinutes(10), scrubber.PositionAt(snapshot, ReportedAt + SettleTimeout));
    }

    [Fact]
    public void DropThePendingTarget_WhenCancelled()
    {
        var scrubber = new PlaybackScrubber(Step, SettleTimeout);
        var snapshot = Snapshot(TimeSpan.FromMinutes(10), isPlaying: false);
        scrubber.Nudge(3, snapshot, ReportedAt);

        scrubber.Cancel();

        Assert.False(scrubber.IsScrubbing);
        Assert.Null(scrubber.Commit(ReportedAt));
        Assert.Equal(TimeSpan.FromMinutes(10), scrubber.PositionAt(snapshot, ReportedAt));
    }

    [Theory]
    [InlineData(245, 1800, "4:05")]
    [InlineData(245, 3700, "0:04:05")]
    [InlineData(3845, 7200, "1:04:05")]
    [InlineData(-3, 1800, "0:00")]
    public void FormatTimes_ToMatchTheTimelinesLength(int seconds, int durationSeconds, string expected)
    {
        Assert.Equal(expected, PlaybackTime.Format(TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(durationSeconds)));
    }
}
