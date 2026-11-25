using HoloNet.TvLauncher.Configuration;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Describes one streaming service shown as a folder on the root screen: its branding, its
/// provider name in HoloNet.Video's library and catalog, and how its CDN artwork is sized. Everything
/// service-specific lives here, so the navigator, cards, and refresh code treat every service
/// the same way. Every service's titles are played by the same browser automation tool (see
/// <see cref="Services.IPrimePlayerLauncher"/>).
/// </summary>
public sealed class StreamingService
{
    /// <summary>Tile art width: 320px tiles at 1080p, doubled so they stay sharp on a 4K TV.</summary>
    public const int TileArtWidth = 640;

    /// <summary>Full-screen hero backdrop width.</summary>
    public const int BackdropArtWidth = 1920;

    public static StreamingService PrimeVideo { get; } = new(
        name: "Prime Video",
        catalogProvider: "PrimeVideo",
        accentColor: "#00A8E1",
        description: "Your Prime Video watchlist, played fullscreen and resumed where you left off.",
        emptySubtitle: "Login required",
        exampleLink: "https://www.amazon.com/gp/video/detail/B0FQM41JFJ",
        isEnabled: options => options.PrimeVideoEnabled,
        sizeArtwork: PrimeVideoArtwork.Sized,
        createLeadingCards: () => [new PrimeAuthActionCardViewModel()]);

    public static StreamingService DisneyPlus { get; } = new(
        name: "Disney+",
        catalogProvider: "DisneyPlus",
        accentColor: "#0063E5",
        description: "Your Disney+ shows, played fullscreen in your signed-in browser.",
        emptySubtitle: "No titles yet",
        exampleLink: "https://www.disneyplus.com/browse/entity-…",
        isEnabled: options => options.DisneyPlusEnabled,
        sizeArtwork: DisneyPlusArtwork.Sized,
        createLeadingCards: () => []);

    public static StreamingService HboMax { get; } = new(
        name: "HBO Max",
        catalogProvider: "HboMax",
        accentColor: "#002BE7",
        description: "Your HBO Max shows and films, played fullscreen in your signed-in browser.",
        emptySubtitle: "No titles yet",
        exampleLink: "https://www.hbomax.com/no/en/show/…",
        isEnabled: options => options.HboMaxEnabled,
        sizeArtwork: HboMaxArtwork.Sized,
        createLeadingCards: () => []);

    /// <summary>Every service, in root-screen order.</summary>
    public static IReadOnlyList<StreamingService> All { get; } = [PrimeVideo, DisneyPlus, HboMax];

    /// <summary>The service with this <see cref="CatalogProvider"/> name, or <c>null</c> if there's none.</summary>
    public static StreamingService? ForProvider(string catalogProvider) =>
        All.FirstOrDefault(service => string.Equals(service.CatalogProvider, catalogProvider, StringComparison.OrdinalIgnoreCase));

    private readonly Func<TvLauncherOptions, bool> _isEnabled;
    private readonly Func<string?, int, string?> _sizeArtwork;
    private readonly Func<IReadOnlyList<IPickerCard>> _createLeadingCards;

    private StreamingService(
        string name,
        string catalogProvider,
        string accentColor,
        string description,
        string emptySubtitle,
        string exampleLink,
        Func<TvLauncherOptions, bool> isEnabled,
        Func<string?, int, string?> sizeArtwork,
        Func<IReadOnlyList<IPickerCard>> createLeadingCards)
    {
        Name = name;
        CatalogProvider = catalogProvider;
        AccentColor = accentColor;
        Description = description;
        EmptySubtitle = emptySubtitle;
        ExampleLink = exampleLink;
        _isEnabled = isEnabled;
        _sizeArtwork = sizeArtwork;
        _createLeadingCards = createLeadingCards;
    }

    /// <summary>Display name, used as the root folder's title.</summary>
    public string Name { get; }

    /// <summary>The provider segment of HoloNet.Video's <c>api/v1/library/{provider}</c> and <c>api/v1/catalog/{provider}/{id}</c> routes.</summary>
    public string CatalogProvider { get; }

    /// <summary>The service's brand colour, shared by every tile in its folder.</summary>
    public string AccentColor { get; }

    /// <summary>The root folder's description, shown in the hero when it's selected.</summary>
    public string Description { get; }

    /// <summary>The root folder's subtitle when none of its titles resolve.</summary>
    public string EmptySubtitle { get; }

    /// <summary>What a link to one of its titles looks like, shown as a placeholder when adding a title.</summary>
    public string ExampleLink { get; }

    /// <summary>The root folder's <see cref="FolderCardViewModel.NavigationKey"/>.</summary>
    public string RootNavigationKey => $"streaming:{CatalogProvider}";

    public bool IsEnabled(TvLauncherOptions options) => _isEnabled(options);

    /// <summary>Fixed action tiles shown before the titles, e.g. Prime Video's login refresh.</summary>
    public IReadOnlyList<IPickerCard> CreateLeadingCards() => _createLeadingCards();

    public string? TileArtUrl(string? url) => _sizeArtwork(url, TileArtWidth);

    public string? BackdropArtUrl(string? url) => _sizeArtwork(url, BackdropArtWidth);
}
