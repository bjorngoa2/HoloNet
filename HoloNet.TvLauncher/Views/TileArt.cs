using System.Windows;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Options for the shared <c>TileArtTemplate</c> (see <c>App.xaml</c>), set on any ancestor of
/// the art and inherited down to it.
/// </summary>
public static class TileArt
{
    /// <summary>
    /// Whether art without an image shows the tile's title as a caption (default <c>true</c>).
    /// The launch splash turns it off because it prints the title large right underneath.
    /// </summary>
    public static readonly DependencyProperty ShowsTitleProperty = DependencyProperty.RegisterAttached(
        "ShowsTitle", typeof(bool), typeof(TileArt),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));

    public static bool GetShowsTitle(DependencyObject element) => (bool)element.GetValue(ShowsTitleProperty);

    public static void SetShowsTitle(DependencyObject element, bool value) => element.SetValue(ShowsTitleProperty, value);
}
