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

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config => config.AddJsonFile("appsettings.json", optional: false))
            .ConfigureServices((context, services) =>
            {
                services.Configure<TvLauncherOptions>(context.Configuration.GetSection("TvLauncher"));
                services.AddHttpClient<IGamesApiClient, GamesApiClient>();
                services.AddSingleton<IGameLauncher, GameLauncher>();
                services.AddSingleton<IGamepadService, GamepadInputService>();
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
