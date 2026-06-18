namespace HoloNet.Photos.Models;

public record PhotoDto(
    string Id,
    string Title,
    string Extension,
    DateTime CreatedAt,
    DateTime ModifiedAt,
    long FileSizeBytes,
    string ReadUrl
);