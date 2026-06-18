namespace HoloNet.Video.Models;

public record VideoDto
(
    string Id,
    string Title,
    string Extension,
    DateTime CreatedAt,
    DateTime ModifiedAt,
    long FileSizeBytes,
    string StreamUrl
);