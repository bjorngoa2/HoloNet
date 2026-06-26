namespace HoloNet.Games.Models;

public record GameMetadata(
    string Title,
    string Platform,
    string? Description,
    int? Year,
    string[]? Genre);
