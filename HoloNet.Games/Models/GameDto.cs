namespace HoloNet.Games.Models;

public record GameDto(
    string Id,
    string Title,
    string Platform,
    string? Description,
    int? Year,
    string[]? Genre,
    string? NetworkPath,
    long? FileSizeBytes,
    string? ThumbnailUrl
);