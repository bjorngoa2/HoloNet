namespace HoloNet.TvLauncher.Models;

/// <summary>
/// Where a media session's playback was at <see cref="UpdatedAt"/>. Players report their position
/// only when something happens (play, pause, seek), so use <see cref="PositionAt"/> to read where
/// playback is now.
/// </summary>
public sealed record PlaybackSnapshot(bool IsPlaying, TimeSpan Position, TimeSpan Duration, DateTimeOffset UpdatedAt)
{
    /// <summary>
    /// Whether the player publishes where playback stands. Some (e.g. Disney+) report only
    /// whether they're playing, leaving position and length at zero.
    /// </summary>
    public bool HasTimeline => Duration > TimeSpan.Zero;

    /// <summary>The playback position at <paramref name="now"/>, kept within the title's length.</summary>
    public TimeSpan PositionAt(DateTimeOffset now)
    {
        var position = IsPlaying && now > UpdatedAt ? Position + (now - UpdatedAt) : Position;
        return Clamp(position);
    }

    /// <summary>Keeps <paramref name="position"/> between the start and end of the title.</summary>
    public TimeSpan Clamp(TimeSpan position)
    {
        if (position < TimeSpan.Zero)
            return TimeSpan.Zero;

        return Duration > TimeSpan.Zero && position > Duration ? Duration : position;
    }
}
