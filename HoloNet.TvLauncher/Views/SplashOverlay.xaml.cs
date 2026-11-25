using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace HoloNet.TvLauncher.Views;

/// <summary>The full-screen launch / status splash (see <see cref="SplashContent"/>).</summary>
public partial class SplashOverlay : UserControl
{
    private static readonly Size BannerArtSize = new(512, 288);
    private static readonly Size PosterArtSize = new(300, 400);
    private static readonly Duration ShowDuration = new(TimeSpan.FromMilliseconds(220));
    private static readonly Duration HideDuration = new(TimeSpan.FromMilliseconds(260));
    private static readonly Duration ProgressCycle = new(TimeSpan.FromMilliseconds(1100));
    private const double EntranceScale = 0.96;
    private const byte GlowAlpha = 0x59;

    private readonly double _indeterminateBarWidth;

    public SplashOverlay()
    {
        InitializeComponent();
        _indeterminateBarWidth = ProgressBar.Width;
    }

    public bool IsShown { get; private set; }

    /// <summary>What's typed in the text field (see <see cref="SplashContent.Input"/>).</summary>
    public string InputText => InputBox.Text;

    /// <summary>
    /// Shows <paramref name="content"/>, fading in if hidden. Calling it while already shown
    /// just swaps the content in place (e.g. "Launching…" turning into an error).
    /// </summary>
    public void Show(SplashContent content)
    {
        ApplyContent(content);

        if (IsShown)
            return;

        IsShown = true;
        Visibility = Visibility.Visible;
        BeginAnimation(OpacityProperty, Motion.FromTo(0, 1, ShowDuration));
        BodyScale.BeginAnimation(ScaleTransform.ScaleXProperty, Motion.FromTo(EntranceScale, 1, ShowDuration));
        BodyScale.BeginAnimation(ScaleTransform.ScaleYProperty, Motion.FromTo(EntranceScale, 1, ShowDuration));
    }

    public void Hide() => _ = HideAsync();

    /// <summary>Fades the splash out, completing once the fade has finished.</summary>
    public Task HideAsync()
    {
        if (!IsShown)
            return Task.CompletedTask;

        IsShown = false;
        var faded = new TaskCompletionSource();
        Motion.FadeTo(this, 0, HideDuration, (_, _) =>
        {
            faded.TrySetResult();

            // Shown again while fading out - keep it up.
            if (IsShown)
                return;

            Visibility = Visibility.Collapsed;
            SetProgressRunning(false);
        });
        return faded.Task;
    }

    private void ApplyContent(SplashContent content)
    {
        var accent = AccentColors.Parse(content.AccentColor);
        GlowStop.Color = Color.FromArgb(GlowAlpha, accent.R, accent.G, accent.B);
        GlowEdgeStop.Color = Color.FromArgb(0, accent.R, accent.G, accent.B);

        Art.Content = content.Card;
        ArtHost.Visibility = content.Card is null ? Visibility.Collapsed : Visibility.Visible;
        var artSize = content.Card?.Shape == CardShape.Poster ? PosterArtSize : BannerArtSize;
        ArtHost.Width = artSize.Width;
        ArtHost.Height = artSize.Height;

        TitleText.Text = content.Title;
        MessageText.Text = content.Message;
        MessageText.Visibility = string.IsNullOrWhiteSpace(content.Message) ? Visibility.Collapsed : Visibility.Visible;
        MessageText.Foreground = (Brush)FindResource(content.IsWarning ? "WarningBrush" : "SubTextBrush");
        ApplyInput(content.Input);
        HintsList.ItemsSource = content.Hints;
        HintsList.Visibility = content.Hints is { Count: > 0 } ? Visibility.Visible : Visibility.Collapsed;

        ProgressTrack.Visibility = content.IsWorking || content.Progress is not null ? Visibility.Visible : Visibility.Hidden;
        if (content.Progress is { } progress)
        {
            ShowProgress(progress);
        }
        else
        {
            ProgressBar.Width = _indeterminateBarWidth;
            SetProgressRunning(content.IsWorking);
        }
    }

    /// <summary>Fills the track from the left to show how far along the work is.</summary>
    private void ShowProgress(double progress)
    {
        SetProgressRunning(false);
        Canvas.SetLeft(ProgressBar, 0);
        ProgressBar.Width = ProgressTrack.Width * Math.Clamp(progress, 0, 1);
    }

    private void ApplyInput(SplashInput? input)
    {
        if (input is null)
        {
            if (InputBox.IsKeyboardFocused)
                Keyboard.ClearFocus();
            InputHost.Visibility = Visibility.Collapsed;
            return;
        }

        InputPlaceholder.Text = input.Placeholder;
        InputBox.Text = input.Text;
        InputHost.Visibility = Visibility.Visible;

        // Focus once the field is laid out, with the text selected so typing or pasting replaces it.
        Dispatcher.BeginInvoke(() =>
        {
            Keyboard.Focus(InputBox);
            InputBox.SelectAll();
        }, System.Windows.Threading.DispatcherPriority.Input);
    }

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e) =>
        InputPlaceholder.Visibility = InputBox.Text.Length == 0 ? Visibility.Visible : Visibility.Hidden;

    private void SetProgressRunning(bool running)
    {
        if (!running)
        {
            ProgressBar.BeginAnimation(Canvas.LeftProperty, null);
            return;
        }

        if (ProgressBar.HasAnimatedProperties)
            return;

        if (!Motion.LoopsEnabled)
        {
            // Held still mid-track, so it still reads as "working".
            Canvas.SetLeft(ProgressBar, (ProgressTrack.Width - _indeterminateBarWidth) / 2);
            return;
        }

        Canvas.SetLeft(ProgressBar, -_indeterminateBarWidth);

        var slide = new DoubleAnimation(-ProgressBar.Width, ProgressTrack.Width, ProgressCycle)
        {
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        ProgressBar.BeginAnimation(Canvas.LeftProperty, slide);
    }
}
