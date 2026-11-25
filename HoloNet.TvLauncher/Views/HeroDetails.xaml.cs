using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HoloNet.TvLauncher.Views;

/// <summary>The focused tile's title, metadata, synopsis and "last session" preview.</summary>
public partial class HeroDetails : UserControl
{
    private const int PreviewDecodeWidth = 896;
    private const double EntranceOffset = 12;

    // Not fully transparent: holding the D-pad replays the entrance per tile, and fading from
    // zero each time would make the text strobe.
    private const double EntranceStartOpacity = 0.25;

    private object? _shownSubject;
    private PreviewKey? _shownPreview;
    private int _previewRequest;

    /// <summary>Identifies one version of a preview file, so an unchanged file isn't decoded again.</summary>
    private readonly record struct PreviewKey(string Path, DateTime WrittenUtc);

    public HeroDetails()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows <paramref name="card"/>. Calling it again for the card already shown just refreshes
    /// the text (e.g. new save stats after a game session) without replaying the entrance.
    /// </summary>
    public void Show(IPickerCard card)
    {
        SetContent(card, card.Title, card.Subtitle, card.Description, card.Details);
        _ = ShowPreviewAsync(card.PreviewImagePath);
    }

    /// <summary>Shows a message in place of a tile's details, e.g. for an empty folder.</summary>
    public void ShowMessage(string title, string message)
    {
        SetContent(title, title, subtitle: null, message, details: null);
        _ = ShowPreviewAsync(null);
    }

    /// <summary>
    /// Decodes the preview off the UI thread (so holding the D-pad across game tiles doesn't
    /// stutter) and only when the file is new or has changed since it was last shown.
    /// </summary>
    private async Task ShowPreviewAsync(string? path)
    {
        var request = ++_previewRequest;
        if (path is null || path != _shownPreview?.Path)
        {
            // Never leave another tile's preview up while this one loads.
            _shownPreview = null;
            PreviewBrush.ImageSource = null;
            PreviewPanel.Visibility = Visibility.Collapsed;
        }

        if (path is null)
            return;

        var shown = _shownPreview;
        var (key, image) = await Task.Run(() =>
        {
            var current = new PreviewKey(path, File.GetLastWriteTimeUtc(path));
            return (current, current == shown ? null : ImageSources.FromFileUncached(path, PreviewDecodeWidth));
        });

        if (request != _previewRequest || key == shown)
            return;

        _shownPreview = image is null ? null : key;
        PreviewBrush.ImageSource = image;
        PreviewPanel.Visibility = image is null ? Visibility.Collapsed : Visibility.Visible;
    }

    private void SetContent(object subject, string title, string? subtitle, string? description, string? details)
    {
        TitleText.Text = title;
        SetOptionalText(MetaText, subtitle);
        SetOptionalText(DescriptionText, description);
        SetOptionalText(DetailsText, details);

        if (Equals(subject, _shownSubject))
            return;

        _shownSubject = subject;
        RootOffset.BeginAnimation(TranslateTransform.YProperty, Motion.FromTo(EntranceOffset, 0, Motion.Standard));
        Root.BeginAnimation(OpacityProperty, Motion.FromTo(EntranceStartOpacity, 1, Motion.Standard));
    }

    private static void SetOptionalText(TextBlock textBlock, string? text)
    {
        textBlock.Text = text ?? string.Empty;
        textBlock.Visibility = string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;
    }
}
