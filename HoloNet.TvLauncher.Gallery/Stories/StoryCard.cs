using HoloNet.TvLauncher.Views;

namespace HoloNet.TvLauncher.Gallery.Stories;

/// <summary>A tile with made-up data, for stories that need one of any kind.</summary>
public sealed class StoryCard(string title) : PickerCardBase
{
    public override string Title { get; } = title;

    public override string Subtitle => string.Empty;

    public string? Glyph { get; init; }

    public string Accent { get; init; } = DefaultAccentColor;

    public CardShape Kind { get; init; } = CardShape.Banner;

    public double? Watched { get; init; }

    public override string? IconGlyph => Glyph;

    public override string AccentColor => Accent;

    public override CardShape Shape => Kind;

    public override double? ProgressPercentage => Watched;

    public override TResult Accept<TResult>(IPickerCardVisitor<TResult> visitor) =>
        throw new NotSupportedException("Gallery tiles can't be opened.");
}
