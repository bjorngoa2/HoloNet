using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Services;
using Microsoft.Extensions.Options;

namespace HoloNet.TvLauncher.Tests;

public class KioskBrowserLauncherShould
{
    [Fact]
    public async Task ReportNotConfigured_WhenTheConfiguredFirefoxIsMissing()
    {
        var launcher = new KioskBrowserLauncher(Options.Create(new TvLauncherOptions
        {
            FirefoxPath = @"C:\does-not-exist\firefox.exe"
        }));

        var result = await launcher.RunAsync("https://www.youtube.com");

        Assert.Equal(KioskBrowserLaunchOutcome.NotConfigured, result.Outcome);
        Assert.False(await launcher.QuitAsync());
    }
}
