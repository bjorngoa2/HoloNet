namespace HoloNet.TvLauncher.Services;

/// <summary>
/// How this run of the launcher started, as Velopack reported it before anything else ran (see
/// <c>App.OnStartup</c>).
/// </summary>
/// <param name="RestartedAfterUpdate">
/// Whether Velopack just started this run after installing an update the player chose to install,
/// so the launcher can confirm the update finished.
/// </param>
public sealed record AppStartup(bool RestartedAfterUpdate);
