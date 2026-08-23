using HoloNet.Games.Configuration;

namespace HoloNet.Games.Tests;

public class GameServiceOptionsShould
{
    // GameServiceOptions.GetGameDirectory() validates that GamePath exists on disk (see
    // MediaDirectory.From), so GamePath must point at a real directory. Path.GetTempPath()
    // is guaranteed to exist on any machine, keeping this test deterministic without
    // requiring fixture setup/teardown.
    private static readonly string GamePath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);

    [Fact]
    public void GetNetworkPath_ReturnUncPath_WhenFileIsUnderGamePath()
    {
        var options = new GameServiceOptions
        {
            GamePath = GamePath,
            NetworkShareRoot = @"\\holonet-server\games"
        };
        var absoluteFilePath = Path.Combine(GamePath, "SNES", "Chrono Trigger", "game.sfc");

        var networkPath = options.GetNetworkPath(absoluteFilePath);

        Assert.Equal(@"\\holonet-server\games\SNES\Chrono Trigger\game.sfc", networkPath);
    }

    [Fact]
    public void GetNetworkPath_ReturnNull_WhenNoShareRootIsConfigured()
    {
        var options = new GameServiceOptions
        {
            GamePath = GamePath,
            NetworkShareRoot = null
        };

        var networkPath = options.GetNetworkPath(Path.Combine(GamePath, "SNES", "game.sfc"));

        Assert.Null(networkPath);
    }
}
