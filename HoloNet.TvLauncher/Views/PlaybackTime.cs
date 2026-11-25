namespace HoloNet.TvLauncher.Views;

/// <summary>Formats playback times the way players print them: <c>4:05</c>, or <c>1:04:05</c> for long titles.</summary>
internal static class PlaybackTime
{
    /// <summary>
    /// Formats <paramref name="time"/>, showing hours whenever <paramref name="duration"/> runs an
    /// hour or more, so every time on one timeline has the same shape.
    /// </summary>
    public static string Format(TimeSpan time, TimeSpan duration)
    {
        var whole = TimeSpan.FromSeconds(Math.Floor(Math.Max(0, time.TotalSeconds)));
        return duration.TotalHours >= 1 || whole.TotalHours >= 1
            ? $"{(int)whole.TotalHours}:{whole.Minutes:00}:{whole.Seconds:00}"
            : $"{whole.Minutes}:{whole.Seconds:00}";
    }
}
