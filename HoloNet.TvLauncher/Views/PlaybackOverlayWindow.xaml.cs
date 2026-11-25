using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// What the playback controls show for one moment of playback. Without a timeline (see
/// <see cref="Models.PlaybackSnapshot.HasTimeline"/>) only the play state shows.
/// </summary>
public sealed record PlaybackFrame(bool IsPlaying, TimeSpan Position, TimeSpan Duration, bool IsScrubbing)
{
    public bool HasTimeline => Duration > TimeSpan.Zero;
}

/// <summary>
/// The playback controls' window: a transparent, click-through layer over the fullscreen browser
/// (see <see cref="FloatingWindows"/>). It only draws; <see cref="PlaybackOverlayController"/>
/// decides what to show and when.
/// </summary>
public partial class PlaybackOverlayWindow : Window
{
    private const string PlayGlyph = "\uF5B0";
    private const string PauseGlyph = "\uF8AE";
    private const double ScrubbingThumbScale = 1.35;

    private double _trackWidth;
    private PlaybackFrame? _frame;
    private bool _wasScrubbing;

    private PlaybackOverlayWindow()
    {
        InitializeComponent();
    }

    /// <summary>Whether the controls are showing (or fading in).</summary>
    public bool IsRevealed { get; private set; }

    /// <summary>Creates the (hidden) controls for <paramref name="title"/>, covering <paramref name="reference"/>'s monitor.</summary>
    public static PlaybackOverlayWindow Create(Window reference, string title, string? eyebrow)
    {
        var window = new PlaybackOverlayWindow();
        window.TitleText.Text = title;
        window.EyebrowText.Text = eyebrow;
        window.EyebrowText.Visibility = string.IsNullOrWhiteSpace(eyebrow) ? Visibility.Collapsed : Visibility.Visible;
        FloatingWindows.CoverMonitorOf(window, reference);
        return window;
    }

    public void Reveal()
    {
        if (IsRevealed)
            return;

        IsRevealed = true;
        Show();
        Motion.FadeTo(Root, 1, Motion.Standard);
    }

    /// <summary>
    /// Fades the controls out, then hides the window, so nothing is composited over the video
    /// while the controls are away.
    /// </summary>
    public void Conceal()
    {
        if (!IsRevealed)
            return;

        IsRevealed = false;
        Motion.FadeTo(Root, 0, Motion.Standard, (_, _) =>
        {
            // Revealed again while fading out - keep it up.
            if (!IsRevealed)
                Hide();
        });
    }

    public void SetHints(IReadOnlyList<ButtonHint> hints) => HintsList.ItemsSource = hints;

    public void Render(PlaybackFrame frame)
    {
        _frame = frame;
        StateGlyph.Text = frame.IsPlaying ? PauseGlyph : PlayGlyph;
        var timelineVisibility = frame.HasTimeline ? Visibility.Visible : Visibility.Hidden;
        Track.Visibility = timelineVisibility;
        TimesRow.Visibility = timelineVisibility;
        ElapsedText.Text = PlaybackTime.Format(frame.Position, frame.Duration);
        RemainingText.Text = $"−{PlaybackTime.Format(frame.Duration - frame.Position, frame.Duration)}";
        ScrubText.Text = ElapsedText.Text;

        var progress = frame.Duration > TimeSpan.Zero ? Math.Clamp(frame.Position / frame.Duration, 0, 1) : 0;
        var x = progress * _trackWidth;
        ElapsedFill.Width = x;
        Canvas.SetLeft(Thumb, x - Thumb.Width / 2);
        Canvas.SetLeft(ScrubBubble, Math.Clamp(x - ScrubBubble.ActualWidth / 2, 0, Math.Max(0, _trackWidth - ScrubBubble.ActualWidth)));

        if (frame.IsScrubbing != _wasScrubbing)
        {
            _wasScrubbing = frame.IsScrubbing;
            var scale = frame.IsScrubbing ? ScrubbingThumbScale : 1;
            ThumbScale.BeginAnimation(ScaleTransform.ScaleXProperty, Motion.To(scale, Motion.Standard));
            ThumbScale.BeginAnimation(ScaleTransform.ScaleYProperty, Motion.To(scale, Motion.Standard));
            Motion.FadeTo(ScrubBubble, frame.IsScrubbing ? 1 : 0, Motion.Standard);
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        FloatingWindows.MakeClickThrough(this);
    }

    private void Track_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _trackWidth = e.NewSize.Width;
        RenderAgain();
    }

    // The bubble is centred on the thumb, so it moves whenever its text changes width.
    private void ScrubBubble_SizeChanged(object sender, SizeChangedEventArgs e) => RenderAgain();

    private void RenderAgain()
    {
        if (_frame is not null)
            Render(_frame);
    }
}
