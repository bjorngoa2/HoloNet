using System.Windows;
using System.Windows.Media.Animation;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Shared motion tokens, so every transition in the launcher moves with the same rhythm: short
/// decelerating eases (fast out of the gate, settling gently), the curve TV platforms use
/// because it makes focus feel immediately responsive to a D-pad press.
/// The XAML equivalents live in <c>Themes/Theme.xaml</c> (<c>FocusDuration</c>, <c>StandardEase</c>); focus
/// timing exists only there, since only the tile template animates focus.
/// </summary>
public static class Motion
{
    public static readonly Duration Standard = new(TimeSpan.FromMilliseconds(260));
    public static readonly Duration Crossfade = new(TimeSpan.FromMilliseconds(450));

    public static readonly IEasingFunction Ease = CreateEase();

    /// <summary>
    /// Whether looping animations (the splash's sliding progress bar) run. Turned off by hosts
    /// that capture still images of the views, such as the screenshot tests, so every capture of
    /// the same state looks the same.
    /// </summary>
    public static bool LoopsEnabled { get; set; } = true;

    public static DoubleAnimation To(double value, Duration duration) => new(value, duration) { EasingFunction = Ease };

    public static DoubleAnimation FromTo(double from, double to, Duration duration) =>
        new(from, to, duration) { EasingFunction = Ease };

    public static void FadeTo(UIElement element, double opacity, Duration duration, EventHandler? completed = null)
    {
        var animation = To(opacity, duration);
        if (completed is not null)
            animation.Completed += completed;

        element.BeginAnimation(UIElement.OpacityProperty, animation);
    }

    private static IEasingFunction CreateEase()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        ease.Freeze();
        return ease;
    }
}
