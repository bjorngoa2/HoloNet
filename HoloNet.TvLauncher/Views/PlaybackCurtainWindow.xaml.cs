using System.Windows;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// An always-on-top copy of the launch splash that hides browser automation (PrimePlayer opening
/// Firefox, clicking Play, going fullscreen) until playback has started, then fades out to reveal
/// the playing video - so starting a title looks like a native app opening rather than a browser
/// being driven.
/// </summary>
/// <remarks>
/// The window is click-through and never activates (see <see cref="FloatingWindows"/>):
/// PrimePlayer's synthesized mouse clicks and keystrokes must land on the browser underneath.
/// </remarks>
public partial class PlaybackCurtainWindow : Window
{
    private PlaybackCurtainWindow()
    {
        InitializeComponent();
    }

    /// <summary>Shows <paramref name="content"/> on top of everything on <paramref name="covered"/>'s monitor.</summary>
    public static PlaybackCurtainWindow Cover(Window covered, SplashContent content)
    {
        var curtain = new PlaybackCurtainWindow();
        FloatingWindows.CoverMonitorOf(curtain, covered);
        curtain.Show();
        curtain.Splash.Show(content);
        return curtain;
    }

    /// <summary>Fades out, revealing what's underneath, and closes.</summary>
    public async Task LiftAsync()
    {
        await Splash.HideAsync();
        Close();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        FloatingWindows.MakeClickThrough(this);
    }
}