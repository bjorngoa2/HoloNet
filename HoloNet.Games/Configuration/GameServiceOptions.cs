using HoloNet.Shared.Helpers;

namespace HoloNet.Games.Configuration;

public class GameServiceOptions
{
    public string GamePath { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;

    public MediaDirectory GetGameDirectory() => MediaDirectory.From(GamePath);
    public ServiceBaseUrl GetBaseUrl() => ServiceBaseUrl.From(BaseUrl);
}
