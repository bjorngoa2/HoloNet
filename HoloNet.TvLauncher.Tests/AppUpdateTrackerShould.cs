using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Services;

namespace HoloNet.TvLauncher.Tests;

public class AppUpdateTrackerShould
{
    private readonly FakeAppUpdateService _service = new();
    private readonly ManualTimeProvider _time = new();

    [Fact]
    public async Task OfferAnUpdateFoundInTheBackground()
    {
        var tracker = Tracker();
        var changes = 0;
        tracker.Changed += () => changes++;
        _service.Next = Ready("0.6.0");

        await tracker.CheckInBackgroundAsync();

        Assert.True(tracker.IsPending);
        Assert.Equal("0.6.0", tracker.PendingUpdate?.NewVersion);
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task SkipBackgroundChecks_WhenAutomaticUpdatesAreOff_ButStillCheckWhenAsked()
    {
        var tracker = Tracker(new TvLauncherOptions { AutoUpdateEnabled = false });
        _service.Next = Ready("0.6.0");

        await tracker.CheckInBackgroundAsync();
        Assert.Equal(0, _service.Checks);

        var result = await tracker.CheckNowAsync();
        Assert.Equal(AppUpdateCheckOutcome.UpdateReady, result.Outcome);
        Assert.True(tracker.IsPending);
    }

    [Fact]
    public async Task SkipAScreensaverCheck_OnlyWhileTheLastCheckIsRecent()
    {
        var tracker = Tracker();
        await tracker.CheckInBackgroundAsync();

        _time.Advance(TimeSpan.FromMinutes(59));
        await tracker.CheckInBackgroundAsync(AppUpdateTracker.ScreensaverCheckMinimumAge);
        Assert.Equal(1, _service.Checks);

        _time.Advance(TimeSpan.FromMinutes(1));
        await tracker.CheckInBackgroundAsync(AppUpdateTracker.ScreensaverCheckMinimumAge);
        Assert.Equal(2, _service.Checks);
    }

    [Fact]
    public async Task ShareOneCheck_BetweenOverlappingRequests()
    {
        var tracker = Tracker();
        var check = new TaskCompletionSource<AppUpdateCheckResult>();
        _service.Pending = check;

        var background = tracker.CheckInBackgroundAsync();
        var manual = tracker.CheckNowAsync();
        check.SetResult(Ready("0.6.0"));
        await background;

        Assert.Equal(AppUpdateCheckOutcome.UpdateReady, (await manual).Outcome);
        Assert.Equal(1, _service.Checks);

        // Once it's done, the next request checks again rather than reusing the old answer.
        _service.Pending = null;
        _service.Next = new AppUpdateCheckResult(AppUpdateCheckOutcome.UpToDate);
        await tracker.CheckNowAsync();
        Assert.Equal(2, _service.Checks);
    }

    [Fact]
    public async Task KeepALaterVersionPutOff_UntilANewerOneOrAManualCheck()
    {
        var tracker = Tracker();
        _service.Next = Ready("0.6.0");
        await tracker.CheckInBackgroundAsync();

        tracker.Dismiss();
        Assert.False(tracker.IsPending);

        await tracker.CheckInBackgroundAsync();
        Assert.False(tracker.IsPending);

        await tracker.CheckNowAsync();
        Assert.True(tracker.IsPending);

        tracker.Dismiss();
        _service.Next = Ready("0.7.0");
        await tracker.CheckInBackgroundAsync();
        Assert.True(tracker.IsPending);
        Assert.Equal("0.7.0", tracker.PendingUpdate?.NewVersion);
    }

    [Theory]
    [InlineData(AppUpdateCheckOutcome.UpToDate)]
    [InlineData(AppUpdateCheckOutcome.NotInstalled)]
    [InlineData(AppUpdateCheckOutcome.Failed)]
    public async Task ReportWhyThereIsNoUpdate_WhenAsked(AppUpdateCheckOutcome outcome)
    {
        var tracker = Tracker();
        _service.Next = new AppUpdateCheckResult(outcome);

        var result = await tracker.CheckNowAsync();

        Assert.Equal(outcome, result.Outcome);
        Assert.False(tracker.IsPending);
    }

    private AppUpdateTracker Tracker(TvLauncherOptions? options = null) =>
        new(_service, options ?? new TvLauncherOptions(), _time);

    private static AppUpdateCheckResult Ready(string version) =>
        new(AppUpdateCheckOutcome.UpdateReady, new AppUpdateInfo("0.5.0", version, null, null!));

    private sealed class FakeAppUpdateService : IAppUpdateService
    {
        public AppUpdateCheckResult Next { get; set; } = new(AppUpdateCheckOutcome.UpToDate);

        /// <summary>When set, checks wait for this instead of answering <see cref="Next"/> at once.</summary>
        public TaskCompletionSource<AppUpdateCheckResult>? Pending { get; set; }

        public int Checks { get; private set; }

        public string CurrentVersion => "0.5.0";

        public Task<AppUpdateCheckResult> CheckAndDownloadUpdateAsync(CancellationToken cancellationToken = default)
        {
            Checks++;
            return Pending?.Task ?? Task.FromResult(Next);
        }

        public void ApplyUpdateAndRestart(AppUpdateInfo updateInfo) => throw new NotSupportedException();
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 11, 25, 20, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
