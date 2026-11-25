using System.Windows.Threading;
using HoloNet.TvLauncher.Services;

namespace HoloNet.TvLauncher.Views;

/// <summary>How watching ended (see <see cref="PlaybackOverlayController.RunAsync"/>).</summary>
public enum PlaybackExit
{
    /// <summary>The player went away, e.g. the browser was closed.</summary>
    Ended,

    /// <summary>The viewer held the quit combo; playback is paused.</summary>
    ReturnedHome
}

/// <summary>
/// Runs the controller-driven playback controls for media playing in a browser (see
/// <see cref="IPlaybackSession"/>), following TV player conventions: any button brings the
/// controls up, Confirm plays/pauses, the D-pad skips 10 seconds per press (see
/// <see cref="PlaybackScrubber"/>, or <see cref="PlayerKeys"/> for players without a timeline),
/// Cancel hides them, and they fade away on their own while
/// playing. They stay up while paused, so paused video always shows where it stands.
/// </summary>
internal sealed class PlaybackOverlayController : IDisposable
{
    private static readonly TimeSpan SkipStep = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ScrubCommitDelay = TimeSpan.FromMilliseconds(700);
    private static readonly TimeSpan SeekSettleTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan AutoHideDelay = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(250);

    private readonly IPlaybackSession _session;
    private readonly PlaybackOverlayWindow _window;
    private readonly Func<GamepadKind> _controllerKind;
    private readonly PlaybackScrubber _scrubber = new(SkipStep, SeekSettleTimeout);
    private readonly DispatcherTimer _refreshTimer = new() { Interval = RefreshInterval };
    private readonly DispatcherTimer _autoHideTimer = new() { Interval = AutoHideDelay };
    private readonly DispatcherTimer _scrubCommitTimer = new() { Interval = ScrubCommitDelay };
    private readonly TaskCompletionSource<PlaybackExit> _finished = new();

    // Play/pause takes the player a moment to report, so the controls show the requested state
    // until it does.
    private bool? _requestedPlaying;
    private bool _reportedPlaying;
    private (GamepadKind Kind, bool IsPlaying)? _hintsFor;

    public PlaybackOverlayController(IPlaybackSession session, PlaybackOverlayWindow window, Func<GamepadKind> controllerKind)
    {
        _session = session;
        _window = window;
        _controllerKind = controllerKind;
        _reportedPlaying = session.Current.IsPlaying;

        _refreshTimer.Tick += (_, _) => Render();
        _autoHideTimer.Tick += (_, _) => AutoHide();
        _scrubCommitTimer.Tick += (_, _) => _ = CommitScrubAsync();
        _session.Changed += OnSessionChanged;
        _session.Ended += OnSessionEnded;
    }

    private bool IsPlaying => _requestedPlaying ?? _session.Current.IsPlaying;

    /// <summary>Shows the controls briefly, then runs them until playback ends or the viewer returns home.</summary>
    public Task<PlaybackExit> RunAsync()
    {
        Reveal();
        return _finished.Task;
    }

    public void HandleButton(GamepadButton button)
    {
        switch (button)
        {
            case GamepadButton.Quit:
                _ = ReturnHomeAsync();
                return;
            case GamepadButton.Cancel when _scrubber.IsScrubbing:
                _scrubCommitTimer.Stop();
                _scrubber.Cancel();
                break;
            case GamepadButton.Cancel:
                Conceal();
                return;
            case GamepadButton.Confirm when _scrubber.IsScrubbing:
                _ = CommitScrubAsync();
                break;
            case GamepadButton.Confirm:
                _ = TogglePlayPauseAsync();
                break;
            case GamepadButton.Left:
                Scrub(-1);
                break;
            case GamepadButton.Right:
                Scrub(1);
                break;
        }

        Reveal();
    }

    public void Dispose()
    {
        _refreshTimer.Stop();
        _autoHideTimer.Stop();
        _scrubCommitTimer.Stop();
        _session.Changed -= OnSessionChanged;
        _session.Ended -= OnSessionEnded;
        _window.Close();
    }

    private void Scrub(int steps)
    {
        // No position to seek to: let the player skip itself (see PlayerKeys).
        if (!_session.Current.HasTimeline)
        {
            PlayerKeys.Skip(steps);
            return;
        }

        _scrubber.Nudge(steps, _session.Current, DateTimeOffset.Now);
        _scrubCommitTimer.Stop();
        _scrubCommitTimer.Start();
    }

    private async Task CommitScrubAsync()
    {
        _scrubCommitTimer.Stop();
        if (_scrubber.Commit(DateTimeOffset.Now) is not { } target)
            return;

        Render();
        await _session.SeekAsync(target);
    }

    private async Task TogglePlayPauseAsync()
    {
        var play = !IsPlaying;
        _requestedPlaying = play;
        Render();

        var accepted = play ? await _session.PlayAsync() : await _session.PauseAsync();
        if (!accepted && _requestedPlaying == play)
        {
            _requestedPlaying = null;
            Render();
        }
    }

    private async Task ReturnHomeAsync()
    {
        _scrubCommitTimer.Stop();
        await _session.PauseAsync();
        _finished.TrySetResult(PlaybackExit.ReturnedHome);
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        var isPlaying = _session.Current.IsPlaying;
        if (_requestedPlaying == isPlaying)
            _requestedPlaying = null;

        // Paused or resumed from elsewhere (the site's own controls, media keys): show it.
        var changedElsewhere = isPlaying != _reportedPlaying && _requestedPlaying is null;
        _reportedPlaying = isPlaying;
        if (changedElsewhere)
            Reveal();
        else
            Render();
    }

    private void OnSessionEnded(object? sender, EventArgs e) => _finished.TrySetResult(PlaybackExit.Ended);

    private void Reveal()
    {
        Render();
        _window.Reveal();
        _refreshTimer.Start();
        _autoHideTimer.Stop();
        _autoHideTimer.Start();
    }

    private void Conceal()
    {
        _autoHideTimer.Stop();
        _refreshTimer.Stop();
        _window.Conceal();
    }

    private void AutoHide()
    {
        if (IsPlaying && !_scrubber.IsScrubbing)
            Conceal();
    }

    private void Render()
    {
        var snapshot = _session.Current;
        var isPlaying = IsPlaying;
        _window.Render(new PlaybackFrame(
            isPlaying,
            _scrubber.PositionAt(snapshot, DateTimeOffset.Now),
            snapshot.Duration,
            _scrubber.IsScrubbing));

        var kind = _controllerKind();
        if (_hintsFor == (kind, isPlaying))
            return;

        _hintsFor = (kind, isPlaying);
        _window.SetHints(
        [
            ButtonPrompts.Hint(kind, PromptButton.Confirm, isPlaying ? "Pause" : "Play"),
            ButtonPrompts.Hint(kind, PromptButton.LeftRight, "Skip 10s"),
            ButtonPrompts.Hint(kind, PromptButton.Cancel, "Hide"),
            ButtonPrompts.QuitHint(kind, "Home")
        ]);
    }
}
