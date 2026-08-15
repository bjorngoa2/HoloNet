namespace HoloNet.TvLauncher.Models;

/// <summary>
/// Mirrors <c>HoloNet.Games.Models.LaunchIntentDto</c>.
/// </summary>
public record LaunchIntentDto(
    string GameId,
    string Title,
    string Platform,
    string NetworkPath
);
