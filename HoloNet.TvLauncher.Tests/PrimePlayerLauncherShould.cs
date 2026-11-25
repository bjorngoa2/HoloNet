using HoloNet.TvLauncher.Services;

namespace HoloNet.TvLauncher.Tests;

public class PrimePlayerLauncherShould
{
    private const string LauncherFolder = @"C:\Users\Tv\AppData\Local\HoloNetTvLauncher\current\";

    [Fact]
    public void FindABundledToolNextToTheLauncher_NotInTheWorkingDirectory() =>
        Assert.Equal(
            @"C:\Users\Tv\AppData\Local\HoloNetTvLauncher\current\HoloNet.PrimePlayer.exe",
            PrimePlayerLauncher.ResolveToolPath("HoloNet.PrimePlayer.exe", LauncherFolder));

    [Fact]
    public void UseAnAbsoluteToolPathAsIs() =>
        Assert.Equal(
            @"D:\Tools\HoloNet.PrimePlayer.exe",
            PrimePlayerLauncher.ResolveToolPath(@"D:\Tools\HoloNet.PrimePlayer.exe", LauncherFolder));
}
