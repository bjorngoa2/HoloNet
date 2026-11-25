using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HoloNet.TvLauncher.Views;

namespace HoloNet.TvLauncher.Gallery.Stories;

/// <summary>Home screen tiles (the <c>TileTemplate</c> in <c>Themes/Theme.xaml</c>) in each of their states.</summary>
public static class TileStories
{
    private static readonly Size RowSize = new(1920, 480);

    public static IReadOnlyList<Story> All { get; } =
    [
        Row("Banners", () =>
        [
            new StoryCard("Games") { Glyph = TileIcons.Game },
            new StoryCard("Prime Video") { Glyph = TileIcons.Video, Accent = "#00A8E1", IsSelected = true },
            new StoryCard("Spotify Music") { Accent = "#1DB954" },
            new StoryCard("Ambient clock") { Glyph = TileIcons.Check, Accent = SettingsMenu.AccentColor },
            new StoryCard("YouTube") { Glyph = TileIcons.Play, Accent = "#FF0033", IsMoving = true }
        ]),
        Row("Posters", () =>
        [
            new StoryCard("Jak II") { Glyph = TileIcons.Game, Accent = "#7A4FD6", Kind = CardShape.Poster },
            new StoryCard("Sly Cooper") { Glyph = TileIcons.Game, Accent = "#2E7DE0", Kind = CardShape.Poster, IsSelected = true },
            new StoryCard("Reacher") { Glyph = TileIcons.Video, Accent = "#00A8E1", Kind = CardShape.Poster, Watched = 35 },
            new StoryCard("Andor") { Glyph = TileIcons.Video, Accent = "#113CCF", Kind = CardShape.Poster, Watched = 80, IsSelected = true }
        ])
    ];

    private static Story Row(string name, Func<IReadOnlyList<StoryCard>> cards) => new("Tiles", name, RowSize, () =>
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(96, 0, 96, 0), VerticalAlignment = VerticalAlignment.Center };
        var template = (DataTemplate)Application.Current.FindResource("TileTemplate");
        foreach (var card in cards())
            row.Children.Add(new ContentPresenter { Content = card, ContentTemplate = template });

        return new Grid
        {
            Width = RowSize.Width,
            Height = RowSize.Height,
            Background = (Brush)Application.Current.FindResource("BackgroundBrush"),
            Children = { row }
        };
    });
}
