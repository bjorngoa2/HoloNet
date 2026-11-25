using System.Windows;
using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Services;
using HoloNet.TvLauncher.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Velopack;

namespace HoloNet.TvLauncher;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // MUST be the very first thing that runs in the process — Velopack uses this hook to
        // detect install/uninstall/update lifecycle events (e.g. it briefly relaunches with a
        // special flag right after an update is applied, purely to fix up shortcuts) and
        // short-circuits back out before any of the app's own startup logic below runs. Only
        // does anything when this is a Velopack-installed copy (i.e. launched via the release's
        // Setup.exe) — a portable/dev build just runs straight through as a no-op.
        VelopackApp.Build().Run();

        base.OnStartup(e);

        // PS2 icon.sys save titles are Shift-JIS encoded; that codepage isn't included by
        // default in .NET (System.Text.Encoding.CodePages ships it) — must be registered before
        // any Encoding.GetEncoding("shift_jis") call (see Ps2MemoryCardReader).
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        // The content root (where appsettings*.json are read from) is the launcher's own folder,
        // not the working directory - which is wherever a shortcut, autostart entry or shell
        // happened to start it from.
        _host = Host.CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureAppConfiguration(config => config
                .AddJsonFile("appsettings.json", optional: false)
                // Optional, gitignored local override — lets you point GamesApiBaseUrl at a
                // local Games API instance (e.g. http://localhost:5046/api/v1/games) without
                // touching the committed production appsettings.json.
                .AddJsonFile("appsettings.local.json", optional: true))
            .ConfigureServices((context, services) =>
            {
                services.AddOptions<TvLauncherOptions>()
                    .Bind(context.Configuration.GetSection("TvLauncher"))
                    .Validate(
                        o => o.UpdateCheckIntervalHours >= TvLauncherOptions.MinimumUpdateCheckIntervalHours,
                        $"TvLauncher:UpdateCheckIntervalHours must be at least {TvLauncherOptions.MinimumUpdateCheckIntervalHours}.")
                    .ValidateOnStart();
                services.AddHttpClient<IGamesApiClient, GamesApiClient>();
                services.AddHttpClient<IMediaCatalogApiClient, MediaCatalogApiClient>();
                services.AddHttpClient<IMediaLibraryApiClient, MediaLibraryApiClient>();
                services.AddHttpClient<IWeatherService, OpenMeteoWeatherService>();
                services.AddHttpClient<IPlaceNameResolver, BigDataCloudPlaceNameResolver>();
                services.AddSingleton<IDeviceLocator, WindowsDeviceLocator>();
                services.AddSingleton<IHomeLocationService, HomeLocationService>();
                services.AddSingleton(TimeProvider.System);
                services.AddSingleton<ILauncherSettingsStore, LauncherSettingsStore>();
                services.AddSingleton<IScreensaverScene, AmbientClockScene>();
                services.AddSingleton<IScreensaverScene, BouncingLogoScene>();
                services.AddSingleton<StreamingLibrary>();
                services.AddSingleton<IGameLauncher, GameLauncher>();
                services.AddSingleton<IGamepadService, GamepadInputService>();
                services.AddSingleton<ISaveStatsService, SaveStatsService>();
                services.AddSingleton<ILocationDiscoveryService, LocationDiscoveryService>();
                services.AddSingleton<IGameScreenshotService, GameScreenshotService>();
                services.AddSingleton<IAppUpdateService, AppUpdateService>();
                services.AddSingleton<IPrimeAuthLauncher, PrimeAuthLauncher>();
                services.AddSingleton<IPrimePlayerLauncher, PrimePlayerLauncher>();
                services.AddSingleton<IKioskBrowserLauncher, KioskBrowserLauncher>();
                services.AddSingleton<IMediaSessionService, MediaSessionService>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        // Loaded before the window exists, so the first screen already reflects the saved choices.
        await _host.Services.GetRequiredService<ILauncherSettingsStore>().LoadAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
