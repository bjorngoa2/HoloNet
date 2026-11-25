using System.IO;
using HoloNet.TvLauncher.Views;

namespace HoloNet.TvLauncher.Tests;

/// <summary>
/// Verifies <see cref="ImageSources.FromFileUncached"/> treats an unreadable "last session"
/// screenshot as missing rather than throwing, since it's decoded whenever a game tile is focused.
/// </summary>
public sealed class ImageSourcesShould : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");

    public void Dispose() => File.Delete(_path);

    [Fact]
    public void ReturnNull_ForAMissingFile()
    {
        Assert.Null(ImageSources.FromFileUncached(_path));
    }

    [Fact]
    public void ReturnNull_ForAHalfWrittenFile()
    {
        File.WriteAllBytes(_path, [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A, 0x00]);

        Assert.Null(ImageSources.FromFileUncached(_path));
    }

    [Fact]
    public void ReturnNull_ForAFileLockedByAWriter()
    {
        using var writer = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.None);

        Assert.Null(ImageSources.FromFileUncached(_path));
    }
}
