using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// The ambient glow and artwork behind the home screen, following the focused tile. Artwork
/// crossfades between two image layers, and is only swapped once focus has rested briefly, so
/// holding the D-pad across a row doesn't start (and abandon) a download per tile passed.
/// </summary>
public partial class HeroBackdrop : UserControl
{
    private const int SharpDecodeWidth = 1920;
    private const int BlurredDecodeWidth = 64;
    private const double SharpOpacity = 0.9;
    private const double BlurredOpacity = 0.55;
    private const byte GlowAlpha = 0x66;
    private static readonly TimeSpan SettleDelay = TimeSpan.FromMilliseconds(160);

    private readonly DispatcherTimer _settleTimer;
    private readonly BlurEffect _blur = new() { Radius = 40, RenderingBias = RenderingBias.Performance };
    private Image _front;
    private Image _back;
    private IPickerCard? _pendingCard;
    private string? _shownArtKey;

    public HeroBackdrop()
    {
        InitializeComponent();
        _front = LayerA;
        _back = LayerB;
        _settleTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher) { Interval = SettleDelay };
        _settleTimer.Tick += OnSettled;
    }

    /// <summary>Retints the glow immediately and swaps the artwork once focus settles.</summary>
    public void Show(IPickerCard? card)
    {
        var accent = AccentColors.Parse(card?.AccentColor);
        AnimateGlow(GlowStop, Color.FromArgb(GlowAlpha, accent.R, accent.G, accent.B));
        AnimateGlow(GlowEdgeStop, Color.FromArgb(0, accent.R, accent.G, accent.B));

        _pendingCard = card;
        _settleTimer.Stop();
        _settleTimer.Start();
    }

    private static void AnimateGlow(GradientStop stop, Color color) =>
        stop.BeginAnimation(GradientStop.ColorProperty, new ColorAnimation(color, Motion.Crossfade) { EasingFunction = Motion.Ease });

    private void OnSettled(object? sender, EventArgs e)
    {
        _settleTimer.Stop();

        // Wide backdrop art is shown sharp; otherwise the tile art is blurred into a colour
        // wash, since box art and logos are too small and too busy to show full-screen.
        var (url, blurred) = _pendingCard switch
        {
            { BackdropUrl: { Length: > 0 } backdrop } => (backdrop, false),
            { ThumbnailUrl: { Length: > 0 } thumbnail } => (thumbnail, true),
            _ => (null, false)
        };

        var artKey = url is null ? null : $"{blurred}|{url}";
        if (artKey == _shownArtKey)
            return;

        _shownArtKey = artKey;
        var image = ImageSources.FromUrl(url, blurred ? BlurredDecodeWidth : SharpDecodeWidth);
        if (image is { IsDownloading: true })
        {
            image.DownloadCompleted += (_, _) =>
            {
                if (_shownArtKey == artKey)
                    CrossfadeTo(image, blurred);
            };
            image.DownloadFailed += (_, _) => ClearFailedArt(artKey);
            image.DecodeFailed += (_, _) => ClearFailedArt(artKey);
            return;
        }

        CrossfadeTo(image, blurred);
    }

    /// <summary>
    /// Fades out artwork that couldn't load, rather than leaving the previous tile's art behind
    /// this one, and forgets it so focusing the tile again retries.
    /// </summary>
    private void ClearFailedArt(string? artKey)
    {
        if (_shownArtKey != artKey)
            return;

        _shownArtKey = null;
        CrossfadeTo(null, blurred: false);
    }

    private void CrossfadeTo(BitmapSource? image, bool blurred)
    {
        (_front, _back) = (_back, _front);
        Panel.SetZIndex(_front, 1);
        Panel.SetZIndex(_back, 0);

        _front.Source = image;
        _front.Effect = blurred ? _blur : null;
        Motion.FadeTo(_front, image is null ? 0 : blurred ? BlurredOpacity : SharpOpacity, Motion.Crossfade);
        Motion.FadeTo(_back, 0, Motion.Crossfade);
    }
}
