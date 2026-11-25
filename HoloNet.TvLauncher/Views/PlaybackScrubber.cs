using HoloNet.TvLauncher.Models;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// D-pad seeking for the playback controls, the way TV players do it: each press moves a
/// pending position by one step and the thumb follows straight away, but the player only
/// seeks once the presses stop (see <see cref="Commit"/>) - so tapping right six times jumps
/// a minute in one seek instead of six stuttering ones. After committing, the thumb stays at
/// the target until the player reports the seek, rather than snapping back for a moment.
/// </summary>
internal sealed class PlaybackScrubber(TimeSpan step, TimeSpan settleTimeout)
{
    private TimeSpan? _target;
    private DateTimeOffset? _committedAt;

    /// <summary>Presses have moved the position, but it hasn't been committed yet.</summary>
    public bool IsScrubbing => _target is not null && _committedAt is null;

    /// <summary>Where the controls should show playback at <paramref name="now"/>.</summary>
    public TimeSpan PositionAt(PlaybackSnapshot snapshot, DateTimeOffset now) =>
        _target is { } target && !HasSettled(snapshot, now) ? target : snapshot.PositionAt(now);

    /// <summary>Moves the pending position by <paramref name="steps"/> steps (negative rewinds).</summary>
    public TimeSpan Nudge(int steps, PlaybackSnapshot snapshot, DateTimeOffset now)
    {
        var target = snapshot.Clamp(PositionAt(snapshot, now) + step * steps);
        _target = target;
        _committedAt = null;
        return target;
    }

    /// <summary>Ends scrubbing, returning the position to seek to, or <c>null</c> when not scrubbing.</summary>
    public TimeSpan? Commit(DateTimeOffset now)
    {
        if (!IsScrubbing)
            return null;

        _committedAt = now;
        return _target;
    }

    /// <summary>Drops the pending position without seeking.</summary>
    public void Cancel()
    {
        _target = null;
        _committedAt = null;
    }

    private bool HasSettled(PlaybackSnapshot snapshot, DateTimeOffset now) =>
        _committedAt is { } committedAt && (snapshot.UpdatedAt >= committedAt || now - committedAt >= settleTimeout);
}
