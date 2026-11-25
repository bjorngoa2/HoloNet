using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Tile artwork that loads in the background and fades in once it's ready, like the poster
/// art on streaming apps. Until then (or if it never loads), <see cref="HasImage"/> stays
/// <c>false</c>, so the tile template keeps a readable placeholder (brand colour and title) in
/// place instead of showing an empty tile.
/// </summary>
public sealed class ArtImage : Border
{
    public static readonly DependencyProperty DecodeWidthProperty = DependencyProperty.Register(
        nameof(DecodeWidth), typeof(int), typeof(ArtImage), new PropertyMetadata(0, OnSourceChanged));

    public static readonly DependencyProperty UrlProperty = DependencyProperty.Register(
        nameof(Url), typeof(string), typeof(ArtImage), new PropertyMetadata(null, OnSourceChanged));

    private static readonly DependencyPropertyKey HasImagePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HasImage), typeof(bool), typeof(ArtImage), new PropertyMetadata(false));

    public static readonly DependencyProperty HasImageProperty = HasImagePropertyKey.DependencyProperty;

    private BitmapImage? _loading;

    /// <summary>Width to decode the image at (see <see cref="ImageSources.FromUrl"/>); 0 for full size.</summary>
    public int DecodeWidth
    {
        get => (int)GetValue(DecodeWidthProperty);
        set => SetValue(DecodeWidthProperty, value);
    }

    public string? Url
    {
        get => (string?)GetValue(UrlProperty);
        set => SetValue(UrlProperty, value);
    }

    public bool HasImage => (bool)GetValue(HasImageProperty);

    private static void OnSourceChanged(DependencyObject element, DependencyPropertyChangedEventArgs e) =>
        ((ArtImage)element).Load();

    private void Load()
    {
        ClearImage();

        var image = ImageSources.FromUrl(Url, DecodeWidth);
        _loading = image;
        if (image is null)
            return;

        if (!image.IsDownloading)
        {
            // Already in WPF's image cache - show it straight away, without a fade.
            Display(image, fadeIn: false);
            return;
        }

        image.DecodeFailed += (_, _) =>
        {
            if (ReferenceEquals(image, _loading))
                ClearImage();
        };
        image.DownloadCompleted += (_, _) =>
        {
            // Ignore a download that finished after the URL changed again.
            if (ReferenceEquals(image, _loading))
                Display(image, fadeIn: true);
        };
    }

    private void ClearImage()
    {
        Background = null;
        SetValue(HasImagePropertyKey, false);
    }

    private void Display(BitmapImage image, bool fadeIn)
    {
        Background = new ImageBrush(image) { Stretch = Stretch.UniformToFill };
        SetValue(HasImagePropertyKey, true);

        if (fadeIn)
            BeginAnimation(OpacityProperty, Motion.FromTo(0, 1, Motion.Standard));
    }
}
