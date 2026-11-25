using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Keeps the mouse pointer hidden over the launcher, which is driven by a gamepad and has no
/// use for it. A parked white arrow is a static, high-contrast spot that would otherwise sit on
/// top of the screensaver for hours (an OLED burn-in risk), and PrimePlayer's synthesized clicks
/// can leave it in the middle of the screen. Moving the mouse shows the pointer again, so a mouse
/// still works for the odd Windows dialog, and it hides again after a few seconds without
/// movement or as soon as the player uses the gamepad (see <see cref="Hide"/>).
/// </summary>
public sealed class CursorAutoHider : IDisposable
{
    private static readonly TimeSpan HideDelay = TimeSpan.FromSeconds(3);

    private readonly Window _window;
    private readonly DispatcherTimer _hideTimer;
    private Point? _lastScreenPosition;

    public CursorAutoHider(Window window)
    {
        _window = window;
        _hideTimer = new DispatcherTimer { Interval = HideDelay };
        _hideTimer.Tick += (_, _) => Hide();

        _window.PreviewMouseMove += OnPreviewMouseMove;
        _window.MouseEnter += OnMouseEnterOrLeave;
        _window.MouseLeave += OnMouseEnterOrLeave;

        Hide();
    }

    /// <summary>Hides the pointer now, e.g. because the player just used the gamepad.</summary>
    public void Hide()
    {
        _hideTimer.Stop();

        // ForceCursor so elements with a cursor of their own (a TextBox's I-beam) stay hidden too.
        _window.ForceCursor = true;
        _window.Cursor = Cursors.None;
    }

    private void Show()
    {
        _window.ForceCursor = false;
        _window.ClearValue(FrameworkElement.CursorProperty);
        _hideTimer.Stop();
        _hideTimer.Start();
    }

    private void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        // Windows also sends a mouse move when the window under a still pointer changes (e.g.
        // the launcher coming back to the front after a game), and WPF when layout changes under
        // it, so only an actual change of position counts as the player reaching for the mouse.
        var position = _window.PointToScreen(e.GetPosition(_window));
        var moved = _lastScreenPosition is { } last && last != position;
        _lastScreenPosition = position;

        if (moved)
            Show();
    }

    // The pointer may have been moved elsewhere (by the player or by PrimePlayer's synthesized
    // clicks) while it was over another window, so the next position seen is a fresh baseline.
    private void OnMouseEnterOrLeave(object sender, MouseEventArgs e) => _lastScreenPosition = null;

    public void Dispose()
    {
        _hideTimer.Stop();
        _window.PreviewMouseMove -= OnPreviewMouseMove;
        _window.MouseEnter -= OnMouseEnterOrLeave;
        _window.MouseLeave -= OnMouseEnterOrLeave;
    }
}
