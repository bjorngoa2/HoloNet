namespace HoloNet.TvLauncher.Models;

/// <summary>
/// Save-file stats for a single game, read from a PCSX2 memory card image, shown as hover info
/// on a game card in the picker. All fields are optional since not every configured game has
/// every stat, and a game with no <see cref="SaveStatsMapping"/> configured at all simply won't
/// have one of these produced.
/// </summary>
public record SaveStats(int? Currency, string? CurrencyLabel, TimeSpan? Playtime);
