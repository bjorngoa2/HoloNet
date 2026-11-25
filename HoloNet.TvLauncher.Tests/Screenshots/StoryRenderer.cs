using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HoloNet.TvLauncher.Gallery.Stories;

namespace HoloNet.TvLauncher.Tests.Screenshots;

/// <summary>Renders a <see cref="Story"/> to an image, the way it looks once it has settled on screen.</summary>
internal static class StoryRenderer
{
    // Longer than any entrance animation (the splash's is 220 ms, a tile's focus 180 ms), so a
    // capture shows where things come to rest. Looping animations are held still (see WpfThread).
    private static readonly TimeSpan SettleTime = TimeSpan.FromMilliseconds(800);

    // One story at a time: they share the UI thread, and each waits for its animations to settle.
    private static readonly SemaphoreSlim OneAtATime = new(1, 1);

    public static async Task<BitmapSource> RenderAsync(Story story)
    {
        await OneAtATime.WaitAsync();
        try
        {
            return await WpfThread.RunAsync(() => RenderOnUiThreadAsync(story));
        }
        finally
        {
            OneAtATime.Release();
        }
    }

    private static async Task<BitmapSource> RenderOnUiThreadAsync(Story story)
    {
        var view = story.CreateView();
        view.Width = story.Size.Width;
        view.Height = story.Size.Height;

        // Hosted in a real (off-screen) window, so it's loaded and animates exactly as in the app.
        var window = new Window
        {
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            ShowActivated = false,
            SizeToContent = SizeToContent.WidthAndHeight,
            Left = -20000,
            Top = -20000,
            Content = view
        };

        try
        {
            window.Show();
            await Task.Delay(SettleTime);
            view.UpdateLayout();

            var bitmap = new RenderTargetBitmap((int)story.Size.Width, (int)story.Size.Height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(view);
            bitmap.Freeze();
            return bitmap;
        }
        finally
        {
            window.Close();
        }
    }
}
