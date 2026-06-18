namespace HoloNet.Games.Models;

public record GameMetadata(
    string Title,
    string Platform,
    string? Description,
    int? Year,
    long? FileSize)
{
    public GameMetadata SetFileSize(long fileSize) => this with { FileSize = fileSize };
};