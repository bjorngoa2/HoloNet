namespace HoloNet.Games.Models;

/// <summary>
/// Launch-intent handoff payload for the TV-connected PC: tells it which game file to open
/// (via the network share) and which platform/emulator it belongs to.
/// </summary>
public record LaunchIntentDto(
    string GameId,
    string Title,
    string Platform,
    string NetworkPath
);
