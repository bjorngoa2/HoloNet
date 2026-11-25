using HoloNet.TvLauncher.Models;
using HoloNet.TvLauncher.Services;

namespace HoloNet.TvLauncher.Tests;

/// <summary>An in-memory <see cref="ILauncherSettingsStore"/> that records every save.</summary>
internal sealed class FakeLauncherSettingsStore(LauncherSettings? initial = null) : ILauncherSettingsStore
{
    public LauncherSettings Current { get; private set; } = initial ?? new();

    public List<LauncherSettings> Saved { get; } = [];

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SaveAsync(LauncherSettings settings, CancellationToken cancellationToken = default)
    {
        Current = settings;
        Saved.Add(settings);
        return Task.CompletedTask;
    }
}
