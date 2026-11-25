using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HoloNet.TvLauncher.Tests.Screenshots;

/// <summary>Reads and writes screenshots as PNG files, and compares their pixels.</summary>
internal static class Png
{
    public static void Write(BitmapSource image, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var file = File.Create(path);
        encoder.Save(file);
    }

    public static BitmapSource Read(string path)
    {
        using var file = File.OpenRead(path);
        var frame = BitmapDecoder.Create(file, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
        frame.Freeze();
        return frame;
    }

    /// <summary>
    /// How many pixels differ noticeably between two images of the same size, or <c>null</c> when
    /// their sizes differ. A channel off by a couple of levels (anti-aliasing noise) doesn't count.
    /// </summary>
    public static int? CountDifferentPixels(BitmapSource expected, BitmapSource actual)
    {
        if (expected.PixelWidth != actual.PixelWidth || expected.PixelHeight != actual.PixelHeight)
            return null;

        var a = Pixels(expected);
        var b = Pixels(actual);
        var different = 0;
        for (var i = 0; i < a.Length; i += 4)
        {
            if (Math.Abs(a[i] - b[i]) > ChannelTolerance
                || Math.Abs(a[i + 1] - b[i + 1]) > ChannelTolerance
                || Math.Abs(a[i + 2] - b[i + 2]) > ChannelTolerance
                || Math.Abs(a[i + 3] - b[i + 3]) > ChannelTolerance)
                different++;
        }

        return different;
    }

    private const int ChannelTolerance = 2;

    private static byte[] Pixels(BitmapSource image)
    {
        var converted = new FormatConvertedBitmap(image, PixelFormats.Pbgra32, null, 0);
        var stride = converted.PixelWidth * 4;
        var pixels = new byte[stride * converted.PixelHeight];
        converted.CopyPixels(pixels, stride, 0);
        return pixels;
    }
}
