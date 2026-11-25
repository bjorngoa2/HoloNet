using System.Windows;
using HoloNet.TvLauncher.Models;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// One screensaver look (Strategy pattern). <see cref="ScreensaverController"/> owns when a
/// screensaver shows; a scene only owns what it draws and animates while it's showing, so a new
/// look is just a new scene registered in <c>App.xaml.cs</c>.
/// </summary>
public interface IScreensaverScene
{
    /// <summary>The setting that picks this scene (see <see cref="LauncherSettings.Screensaver"/>).</summary>
    ScreensaverStyle ScreensaverStyle { get; }

    /// <summary>The element drawn over the launcher while this scene runs.</summary>
    FrameworkElement View { get; }

    /// <summary>Starts drawing and animating. Called each time the screensaver appears.</summary>
    void Start();

    /// <summary>Stops every timer and animation, so a hidden scene costs nothing.</summary>
    void Stop();
}
