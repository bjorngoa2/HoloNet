using System.Windows;
using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Services;
using HoloNet.TvLauncher.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HoloNet.TvLauncher;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // PS2 icon.sys save titles are Shift-JIS encoded; that codepage isn't included by
        // default in .NET (System.Text.Encoding.CodePages ships it) — must be registered before
        // any Encoding.GetEncoding("shift_jis") call (see Ps2MemoryCardReader).
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config => config
                .AddJsonFile("appsettings.json", optional: false)
                // Optional, gitignored local override — lets you point GamesApiBaseUrl at a
                // local Games API instance (e.g. http://localhost:5046/api/v1/games) without
                // touching the committed production appsettings.json.
                .AddJsonFile("appsettings.local.json", optional: true))
            .ConfigureServices((context, services) =>
            {
                services.Configure<TvLauncherOptions>(context.Configuration.GetSection("TvLauncher"));
                services.AddHttpClient<IGamesApiClient, GamesApiClient>();
                services.AddSingleton<IGameLauncher, GameLauncher>();
                services.AddSingleton<IGamepadService, GamepadInputService>();
                services.AddSingleton<ISaveStatsService, SaveStatsService>();
                services.AddSingleton<ILocationDiscoveryService, LocationDiscoveryService>();
                services.AddSingleton<IGameScreenshotService, GameScreenshotService>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

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
