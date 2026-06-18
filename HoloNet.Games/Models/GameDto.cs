namespace HoloNet.Games.Models;

public record GameDto(
    string Id,
    string Title,
    string Extension,
    DateTime CreatedAt,
    DateTime ModifiedAt,
    long FileSizeBytes,
    string ReadUrl
);