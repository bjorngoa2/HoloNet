using System.Windows;

namespace HoloNet.TvLauncher.Themes;

/// <summary>
/// The launcher's design tokens and shared templates (<c>Themes/Theme.xaml</c>), for hosts other
/// than the launcher itself, such as the gallery and the screenshot tests, to render its views
/// with the same look.
/// </summary>
public static class Theme
{
    public static Uri Source { get; } = new("pack://application:,,,/HoloNet.TvLauncher;component/Themes/Theme.xaml", UriKind.Absolute);

    /// <summary>Loads a fresh copy of the theme, to merge into a host's application resources.</summary>
    public static ResourceDictionary Load() => new() { Source = Source };
}
