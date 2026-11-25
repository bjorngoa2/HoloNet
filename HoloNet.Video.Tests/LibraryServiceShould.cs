using HoloNet.Shared.Models;
using HoloNet.Video.Abstractions;
using HoloNet.Video.Infrastructure;
using HoloNet.Video.Models;
using HoloNet.Video.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace HoloNet.Video.Tests;

public sealed class LibraryServiceShould : IAsyncLifetime
{
    private const string ReacherLink = "https://prime.test/detail/reacher";
    private static readonly DateTimeOffset Now = new(2026, 11, 23, 20, 0, 0, TimeSpan.Zero);

    private readonly InMemoryVideoDatabase _database = new();
    private readonly FakeCatalogProvider _prime = new(MediaProvider.PrimeVideo, "Prime Video", "https://prime.test/detail/");
    private readonly FakeCatalogProvider _disney = new(MediaProvider.DisneyPlus, "Disney+", "https://disney.test/entity-");
    private readonly LibraryService _library;

    public LibraryServiceShould() =>
        _library = new LibraryService(_database, [new LocalOnlyProvider(), _prime, _disney], new FakeTimeProvider(Now));

    public Task InitializeAsync() =>
        new ServiceCollection()
            .AddSingleton<Microsoft.EntityFrameworkCore.IDbContextFactory<HoloNetVideoDbContext>>(_database)
            .BuildServiceProvider()
            .MigrateVideoDatabaseAsync();

    public Task DisposeAsync()
    {
        _database.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ImportATitle_FromTheServiceItsLinkBelongsTo()
    {
        _prime.Titles["reacher"] = new MediaItem("reacher", MediaProvider.PrimeVideo, "Reacher", MediaEntityType.Season,
            PlaybackType.External, PosterUrl: "https://img.test/reacher.jpg");

        var result = await _library.ImportAsync(ReacherLink, CancellationToken.None);

        var expected = new LibraryTitleDto(MediaProvider.PrimeVideo, "reacher", "Reacher", MediaEntityType.Season, "https://img.test/reacher.jpg", Now);
        Assert.Equal(LibraryImportStatus.Imported, result.Status);
        Assert.Equal(expected, result.Title);
        Assert.Equal([expected], await _library.GetAllAsync(MediaProvider.PrimeVideo, CancellationToken.None));
        Assert.Empty(await _library.GetAllAsync(MediaProvider.DisneyPlus, CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData("https://www.youtube.com/watch?v=abc")]
    public async Task RejectLinks_NoStreamingServiceRecognises(string link)
    {
        var result = await _library.ImportAsync(link, CancellationToken.None);

        Assert.Equal(LibraryImportStatus.UnrecognisedLink, result.Status);
        Assert.Equal("Paste a link to a title on Prime Video or Disney+.", result.Message);
    }

    [Fact]
    public async Task NotAddATitle_TheServiceCantFind()
    {
        var result = await _library.ImportAsync("https://disney.test/entity-missing", CancellationToken.None);

        Assert.Equal(LibraryImportStatus.TitleNotFound, result.Status);
        Assert.StartsWith("Couldn't find that title on Disney+.", result.Message);
        Assert.Empty(await _library.GetAllAsync(provider: null, CancellationToken.None));
    }

    [Fact]
    public async Task ReportUnavailable_WhenTheServiceFails()
    {
        _prime.Failure = new HttpRequestException("503");

        var result = await _library.ImportAsync(ReacherLink, CancellationToken.None);

        Assert.Equal(LibraryImportStatus.ProviderUnavailable, result.Status);
        Assert.Empty(await _library.GetAllAsync(provider: null, CancellationToken.None));
    }

    [Fact]
    public async Task ReportATitleAlreadyInTheLibrary_WithoutLookingItUpAgain()
    {
        _prime.Titles["reacher"] = new MediaItem("reacher", MediaProvider.PrimeVideo, "Reacher", MediaEntityType.Season, PlaybackType.External);
        await _library.ImportAsync(ReacherLink, CancellationToken.None);
        _prime.Titles.Clear();

        var result = await _library.ImportAsync(ReacherLink, CancellationToken.None);

        Assert.Equal(LibraryImportStatus.AlreadyInLibrary, result.Status);
        Assert.Equal("Reacher is already in Prime Video.", result.Message);
    }

    [Fact]
    public async Task RemoveATitle_OnlyOnce()
    {
        _disney.Titles["clone-wars"] = new MediaItem("clone-wars", MediaProvider.DisneyPlus, "The Clone Wars", MediaEntityType.Series, PlaybackType.External);
        await _library.ImportAsync("https://disney.test/entity-clone-wars", CancellationToken.None);

        Assert.True(await _library.RemoveAsync(MediaProvider.DisneyPlus, "clone-wars", CancellationToken.None));
        Assert.False(await _library.RemoveAsync(MediaProvider.DisneyPlus, "clone-wars", CancellationToken.None));
        Assert.Empty(await _library.GetAllAsync(provider: null, CancellationToken.None));
    }

    private sealed class FakeCatalogProvider(MediaProvider provider, string displayName, string linkPrefix) : ICuratedCatalogProvider
    {
        public Dictionary<string, MediaItem> Titles { get; } = [];
        public Exception? Failure { get; set; }

        public MediaProvider Provider => provider;
        public string DisplayName => displayName;

        public string? ParseTitleId(string urlOrId) =>
            urlOrId.StartsWith(linkPrefix, StringComparison.Ordinal) ? urlOrId[linkPrefix.Length..] : null;

        public Task<MediaItem?> GetAsync(string providerMediaId, CancellationToken cancellationToken) =>
            Failure is not null ? Task.FromException<MediaItem?>(Failure) : Task.FromResult(Titles.GetValueOrDefault(providerMediaId));

        public Task<IReadOnlyList<MediaCollection>> GetHomeAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MediaItem>> SearchAsync(string query, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    /// <summary>A catalog the user doesn't curate (like local files), which the library ignores.</summary>
    private sealed class LocalOnlyProvider : IVideoCatalogProvider
    {
        public MediaProvider Provider => MediaProvider.Local;
        public Task<MediaItem?> GetAsync(string providerMediaId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MediaCollection>> GetHomeAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MediaItem>> SearchAsync(string query, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
