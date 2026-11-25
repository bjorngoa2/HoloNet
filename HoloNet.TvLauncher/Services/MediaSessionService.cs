using System.Runtime.InteropServices;
using HoloNet.TvLauncher.Models;
using Windows.Foundation;
using Windows.Media.Control;

namespace HoloNet.TvLauncher.Services;

/// <summary>
/// Finds media playing in other apps through Windows' system media sessions - the same feed
/// that drives the volume flyout's media controls and keyboard media keys. Browsers publish
/// every playing video there (title, timeline, play/pause, seek), whatever the site, so the
/// launcher can show and drive its own playback controls on top of any streaming service
/// without touching the page.
/// </summary>
public interface IMediaSessionService
{
    /// <summary>
    /// Waits up to <paramref name="timeout"/> for a media session published by the app whose
    /// identity contains <paramref name="sourceAppId"/> (e.g. <c>firefox</c>), preferring one
    /// that's playing. Returns <c>null</c> if none appears in time. The session raises its
    /// events on the caller's synchronization context.
    /// </summary>
    Task<IPlaybackSession?> WaitForSessionAsync(string sourceAppId, TimeSpan timeout, CancellationToken cancellationToken = default);
}

/// <summary>Remote control of media playing in another app (see <see cref="IMediaSessionService"/>).</summary>
public interface IPlaybackSession : IDisposable
{
    PlaybackSnapshot Current { get; }

    /// <summary>Playback state or timeline changed (played, paused, seeked).</summary>
    event EventHandler? Changed;

    /// <summary>The app stopped publishing the session, e.g. because the browser was closed.</summary>
    event EventHandler? Ended;

    Task<bool> PlayAsync();

    Task<bool> PauseAsync();

    Task<bool> SeekAsync(TimeSpan position);
}

public sealed class MediaSessionService : IMediaSessionService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    public async Task<IPlaybackSession?> WaitForSessionAsync(string sourceAppId, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var context = SynchronizationContext.Current;
        var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        try
        {
            while (true)
            {
                if (FindSession(manager, sourceAppId) is { } session && TryReadSnapshot(session) is { } snapshot)
                    return new SystemPlaybackSession(manager, session, snapshot, context);

                await Task.Delay(PollInterval, deadline.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private static GlobalSystemMediaTransportControlsSession? FindSession(
        GlobalSystemMediaTransportControlsSessionManager manager,
        string sourceAppId) =>
        manager.GetSessions()
            .Where(session => session.SourceAppUserModelId.Contains(sourceAppId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(session =>
                session.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
            .FirstOrDefault();

    /// <summary>
    /// Reads where <paramref name="session"/> stands, or <c>null</c> while it has nothing to
    /// report: Windows returns no playback info or timeline while the app is updating them, and
    /// the session can go away mid-read.
    /// </summary>
    private static PlaybackSnapshot? TryReadSnapshot(GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            if (session.GetTimelineProperties() is not { } timeline || session.GetPlaybackInfo() is not { } info)
                return null;

            return new PlaybackSnapshot(
                info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
                timeline.Position,
                timeline.EndTime - timeline.StartTime,
                timeline.LastUpdatedTime);
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// One system media session. Windows raises its events on background threads; they're
    /// forwarded to the synchronization context the session was found on, so UI code can
    /// handle them directly.
    /// </summary>
    private sealed class SystemPlaybackSession : IPlaybackSession
    {
        private readonly GlobalSystemMediaTransportControlsSessionManager _manager;
        private readonly GlobalSystemMediaTransportControlsSession _session;
        private readonly SynchronizationContext? _context;
        private readonly string _sourceAppId;
        private PlaybackSnapshot _current;
        private bool _hasEnded;
        private bool _isDisposed;

        public SystemPlaybackSession(
            GlobalSystemMediaTransportControlsSessionManager manager,
            GlobalSystemMediaTransportControlsSession session,
            PlaybackSnapshot initial,
            SynchronizationContext? context)
        {
            _manager = manager;
            _session = session;
            _context = context;
            _sourceAppId = session.SourceAppUserModelId;
            _current = initial;

            _session.PlaybackInfoChanged += OnSessionChanged;
            _session.TimelinePropertiesChanged += OnSessionChanged;
            _manager.SessionsChanged += OnSessionsChanged;
        }

        public PlaybackSnapshot Current => _current;

        public event EventHandler? Changed;

        public event EventHandler? Ended;

        public Task<bool> PlayAsync() => TryAsync(() => _session.TryPlayAsync());

        public Task<bool> PauseAsync() => TryAsync(() => _session.TryPauseAsync());

        public Task<bool> SeekAsync(TimeSpan position) => TryAsync(() => _session.TryChangePlaybackPositionAsync(position.Ticks));

        public void Dispose()
        {
            _isDisposed = true;
            _session.PlaybackInfoChanged -= OnSessionChanged;
            _session.TimelinePropertiesChanged -= OnSessionChanged;
            _manager.SessionsChanged -= OnSessionsChanged;
        }

        /// <summary>Sends a command, treating a session that has just gone away as a refusal.</summary>
        private static async Task<bool> TryAsync(Func<IAsyncOperation<bool>> command)
        {
            try
            {
                return await command();
            }
            catch (COMException)
            {
                return false;
            }
        }

        private void OnSessionChanged<TArgs>(GlobalSystemMediaTransportControlsSession sender, TArgs args) =>
            Post(() =>
            {
                // Mid-update or gone (SessionsChanged reports the end): keep the last snapshot.
                if (TryReadSnapshot(_session) is not { } snapshot)
                    return;

                _current = snapshot;
                Changed?.Invoke(this, EventArgs.Empty);
            });

        private void OnSessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
        {
            if (sender.GetSessions().Any(session => session.SourceAppUserModelId == _sourceAppId))
                return;

            Post(() =>
            {
                if (_hasEnded)
                    return;

                _hasEnded = true;
                Ended?.Invoke(this, EventArgs.Empty);
            });
        }

        /// <summary>Runs <paramref name="action"/> on the session's context, unless the session has been disposed by then.</summary>
        private void Post(Action action)
        {
            if (_context is null)
                RunUnlessDisposed();
            else
                _context.Post(_ => RunUnlessDisposed(), null);

            void RunUnlessDisposed()
            {
                if (!_isDisposed)
                    action();
            }
        }
    }
}
