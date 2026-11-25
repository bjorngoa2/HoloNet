using System.Windows;
using System.Windows.Controls;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Animated scrolling for a <see cref="ScrollViewer"/>, whose own offsets aren't animatable
/// dependency properties. Animating this attached proxy property instead, and forwarding each
/// frame to <see cref="ScrollViewer.ScrollToVerticalOffset"/>, makes the tile grid glide to
/// the focused row like a native TV launcher, rather than jumping.
/// </summary>
public static class SmoothScroll
{
    public static readonly DependencyProperty VerticalOffsetProperty = DependencyProperty.RegisterAttached(
        "VerticalOffset",
        typeof(double),
        typeof(SmoothScroll),
        new PropertyMetadata(0.0, (element, e) => ((ScrollViewer)element).ScrollToVerticalOffset((double)e.NewValue)));

    public static double GetVerticalOffset(ScrollViewer viewer) => (double)viewer.GetValue(VerticalOffsetProperty);

    public static void SetVerticalOffset(ScrollViewer viewer, double value) => viewer.SetValue(VerticalOffsetProperty, value);

    public static void AnimateTo(ScrollViewer viewer, double offset) =>
        viewer.BeginAnimation(VerticalOffsetProperty, Motion.FromTo(viewer.VerticalOffset, offset, Motion.Standard));

    /// <summary>Scrolls instantly, cancelling any scroll animation in flight.</summary>
    public static void JumpTo(ScrollViewer viewer, double offset)
    {
        // Set the base value before removing the animation, so the effective value never
        // drops back to a stale base value in between.
        SetVerticalOffset(viewer, offset);
        viewer.BeginAnimation(VerticalOffsetProperty, null);
        viewer.ScrollToVerticalOffset(offset);
    }
}
