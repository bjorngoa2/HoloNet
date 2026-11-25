using HoloNet.Shared.Filters;
using HoloNet.Shared.HealthChecks;
using HoloNet.Shared.Helpers;
using HoloNet.Shared.Models;
using HoloNet.Video.Abstractions;
using HoloNet.Video.Configuration;
using HoloNet.Video.Infrastructure;
using HoloNet.Video.Models;
using HoloNet.Video.Providers.DisneyPlus;
using HoloNet.Video.Providers.HboMax;
using HoloNet.Video.Providers.Local;
using HoloNet.Video.Providers.PrimeVideo;
using HoloNet.Video.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<VideoServiceOptions>()
    .Bind(builder.Configuration.GetSection("VideoService"))
    .Validate(o => OptionsValidation.IsValid(() => o.GetVideoDirectory()), "VideoService:VideoPath must point to an existing directory.")
    .Validate(o => OptionsValidation.IsValid(() => o.GetBaseUrl()), "VideoService:BaseUrl must be a valid absolute http/https URL.")
    .ValidateOnStart();

builder.Services.AddOptions<PrimeVideoOptions>()
    .Bind(builder.Configuration.GetSection("PrimeVideo"))
    .Validate(o => OptionsValidation.IsValid(() => o.GetBaseUrl()), "PrimeVideo:BaseUrl must be a valid absolute http/https URL.")
    .ValidateOnStart();

builder.Services.AddOptions<DisneyPlusOptions>()
    .Bind(builder.Configuration.GetSection("DisneyPlus"))
    .Validate(o => OptionsValidation.IsValid(() => o.GetBaseUrl()), "DisneyPlus:BaseUrl must be a valid absolute http/https URL.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Locale), "DisneyPlus:Locale is required.")
    .ValidateOnStart();

builder.Services.AddOptions<HboMaxOptions>()
    .Bind(builder.Configuration.GetSection("HboMax"))
    .Validate(o => OptionsValidation.IsValid(() => o.GetBaseUrl()), "HboMax:BaseUrl must be a valid absolute http/https URL.")
    .Validate(o => OptionsValidation.IsValid(() => o.GetPlayBaseUrl()), "HboMax:PlayBaseUrl must be a valid absolute http/https URL.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Country) && !string.IsNullOrWhiteSpace(o.Language), "HboMax:Country and HboMax:Language are required.")
    .ValidateOnStart();

builder.Services.AddScoped<IVideoService, VideoService>();
builder.Services.AddScoped<IVideoCatalogProvider, LocalVideoCatalogProvider>();

builder.Services.AddDbContextFactory<HoloNetVideoDbContext>(options =>
{
    var dbPath = builder.Configuration["PrimeVideo:DbPath"] ?? "data/holonet-video.db";
    var fullPath = Path.IsPathRooted(dbPath) ? dbPath : Path.Combine(AppContext.BaseDirectory, dbPath);
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
    options.UseSqlite($"Data Source={fullPath}");
});

builder.Services.AddSingleton<PrimeVideoSessionStore>();

// Streaming sites serve their catalog pages to browsers, so their clients present as one.
foreach (var streamingClientName in new[] { "PrimeVideo", DisneyPlusProvider.HttpClientName, HboMaxProvider.HttpClientName })
{
    builder.Services.AddHttpClient(streamingClientName)
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All,
        })
        .ConfigureHttpClient(client =>
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"));
}

builder.Services.AddScoped<IVideoCatalogProvider, PrimeVideoProvider>();
builder.Services.AddScoped<IVideoCatalogProvider, DisneyPlusProvider>();
builder.Services.AddScoped<IVideoCatalogProvider, HboMaxProvider>();
builder.Services.AddScoped<ILibraryService, LibraryService>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddOpenApi();
builder.Services.AddHoloNetProblemDetails();
builder.Services.AddHealthChecks()
    .AddCheck("media_directory", new MediaDirectoryHealthCheck(
        builder.Configuration["VideoService:VideoPath"] ?? string.Empty));
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

await app.Services.MigrateVideoDatabaseAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapHealthChecks("api/v1/health").WithName("HealthCheck");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();


// Endpoints
app.MapGet("api/v1/videos", async (IVideoService service) =>
{
    var result = await service.GetAllAsync();

    return Results.Ok(result);
}).WithName("GetVideos");

app.MapGet("api/v1/videos/{id}", async (IVideoService service, string id) =>
{
    var videoMetadata = await service.GetAsync(id);

    if (videoMetadata is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(videoMetadata);
}).WithName("GetVideo").AddEndpointFilter(EndpointFilters.RequireRouteValue("id", "Video id is required."));

app.MapGet("api/v1/videos/{id}/stream", async (string id, IVideoService service) =>
{
    var video = await service.GetStreamAsync(id);

    if (video is null)
    {
        return Results.NotFound();
    }

    return Results.File(video.Stream, VideoFileTypes.GetContentType(video.Extension), enableRangeProcessing: true);

}).WithName("GetVideoStream").AddEndpointFilter(EndpointFilters.RequireRouteValue("id", "Video id is required."));

// Catalog endpoints - aggregate browsing metadata across all registered IVideoCatalogProvider
// implementations (currently Local, Prime Video, Disney+ and HBO Max). This is catalog/browsing only: a MediaItem's
// PlaybackType tells the caller whether it can actually be played in-app (Local) or only linked
// out to (Prime Video) - see HoloNet.Video/Abstractions/IVideoCatalogProvider.cs.
app.MapGet("api/v1/catalog/home", async (IEnumerable<IVideoCatalogProvider> providers, CancellationToken cancellationToken) =>
{
    var collections = new List<MediaCollection>();
    foreach (var provider in providers)
    {
        collections.AddRange(await provider.GetHomeAsync(cancellationToken));
    }

    return Results.Ok(collections);
}).WithName("GetCatalogHome");

app.MapGet("api/v1/catalog/search", async (string q, IEnumerable<IVideoCatalogProvider> providers, CancellationToken cancellationToken) =>
{
    var items = new List<MediaItem>();
    foreach (var provider in providers)
    {
        items.AddRange(await provider.SearchAsync(q, cancellationToken));
    }

    return Results.Ok(items);
}).WithName("SearchCatalog");

app.MapGet("api/v1/catalog/{provider}/{id}", async (string provider, string id, IEnumerable<IVideoCatalogProvider> providers, CancellationToken cancellationToken) =>
{
    if (!TryParseProvider(provider, out var mediaProvider))
    {
        return UnknownProvider(provider);
    }

    var catalogProvider = providers.FirstOrDefault(p => p.Provider == mediaProvider);
    if (catalogProvider is null)
    {
        return Results.NotFound();
    }

    var item = await catalogProvider.GetAsync(id, cancellationToken);
    return item is null ? Results.NotFound() : Results.Ok(item);
}).WithName("GetCatalogItem")
  .AddEndpointFilter(EndpointFilters.RequireRouteValue("provider", "Provider is required."))
  .AddEndpointFilter(EndpointFilters.RequireRouteValue("id", "Media id is required."));

// Library endpoints - the user's hand-picked Prime Video, Disney+ and HBO Max titles, which fill each
// service's folder in the TV launcher (see ILibraryService for why they're curated by hand).
app.MapGet("api/v1/library", async (ILibraryService library, CancellationToken cancellationToken) =>
    Results.Ok(await library.GetAllAsync(provider: null, cancellationToken)))
    .WithName("GetLibrary");

app.MapGet("api/v1/library/{provider}", async (string provider, ILibraryService library, CancellationToken cancellationToken) =>
    TryParseProvider(provider, out var mediaProvider)
        ? Results.Ok(await library.GetAllAsync(mediaProvider, cancellationToken))
        : UnknownProvider(provider))
    .WithName("GetLibraryByProvider")
    .AddEndpointFilter(EndpointFilters.RequireRouteValue("provider", "Provider is required."));

app.MapGet("api/v1/library/{provider}/{id}", async (string provider, string id, ILibraryService library, CancellationToken cancellationToken) =>
{
    if (!TryParseProvider(provider, out var mediaProvider))
    {
        return UnknownProvider(provider);
    }

    var title = await library.GetAsync(mediaProvider, id, cancellationToken);
    return title is null ? Results.NotFound() : Results.Ok(title);
}).WithName("GetLibraryTitle")
  .AddEndpointFilter(EndpointFilters.RequireRouteValue("provider", "Provider is required."))
  .AddEndpointFilter(EndpointFilters.RequireRouteValue("id", "Title id is required."));

app.MapPost("api/v1/library", async (LibraryImportRequest request, ILibraryService library, CancellationToken cancellationToken) =>
{
    var result = await library.ImportAsync(request.UrlOrId, cancellationToken);
    return result.Status switch
    {
        LibraryImportStatus.Imported => Results.Created(
            $"/api/v1/library/{result.Title!.Provider}/{Uri.EscapeDataString(result.Title.ProviderMediaId)}", result.Title),
        LibraryImportStatus.AlreadyInLibrary => Results.Problem(result.Message, statusCode: StatusCodes.Status409Conflict),
        LibraryImportStatus.UnrecognisedLink => Results.Problem(result.Message, statusCode: StatusCodes.Status400BadRequest),
        LibraryImportStatus.TitleNotFound => Results.Problem(result.Message, statusCode: StatusCodes.Status404NotFound),
        _ => Results.Problem(result.Message, statusCode: StatusCodes.Status502BadGateway),
    };
}).WithName("ImportLibraryTitle");

app.MapDelete("api/v1/library/{provider}/{id}", async (string provider, string id, ILibraryService library, CancellationToken cancellationToken) =>
{
    if (!TryParseProvider(provider, out var mediaProvider))
    {
        return UnknownProvider(provider);
    }

    return await library.RemoveAsync(mediaProvider, id, cancellationToken) ? Results.NoContent() : Results.NotFound();
}).WithName("RemoveLibraryTitle")
  .AddEndpointFilter(EndpointFilters.RequireRouteValue("provider", "Provider is required."))
  .AddEndpointFilter(EndpointFilters.RequireRouteValue("id", "Title id is required."));

// Admin endpoints for runtime credential capture - LAN-only, no auth (consistent with the rest
// of HoloNet). Lets the standalone HoloNet.PrimeAuth browser-login tool (run on a machine with a
// display, since this service itself runs headless in Docker) push a freshly captured Prime
// Video session cookie in without a container restart - see PrimeVideoSessionStore.
app.MapPut("api/v1/admin/primevideo/session-cookie", (PrimeVideoSessionCookieRequest request, PrimeVideoSessionStore sessionStore) =>
{
    if (string.IsNullOrWhiteSpace(request.Cookie))
    {
        return Results.Problem("Cookie value is required.", statusCode: StatusCodes.Status400BadRequest);
    }

    sessionStore.Update(request.Cookie);
    return Results.NoContent();
}).WithName("SetPrimeVideoSessionCookie");

app.MapGet("api/v1/admin/primevideo/session-cookie/status", (PrimeVideoSessionStore sessionStore) =>
    Results.Ok(new PrimeVideoSessionCookieStatus(sessionStore.IsConfigured, sessionStore.LastUpdatedUtc)))
    .WithName("GetPrimeVideoSessionCookieStatus");

app.Run();

// Provider route segments are enum names ("PrimeVideo"); numeric strings would also parse, so
// only defined values are accepted.
static bool TryParseProvider(string value, out MediaProvider provider) =>
    Enum.TryParse(value, ignoreCase: true, out provider) && Enum.IsDefined(provider) && !int.TryParse(value, out _);

static IResult UnknownProvider(string provider) =>
    Results.Problem($"Unknown provider '{provider}'.", statusCode: StatusCodes.Status400BadRequest);