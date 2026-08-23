using HoloNet.Games.Configuration;
using HoloNet.Games.Services;
using HoloNet.Shared.Filters;
using HoloNet.Shared.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Optional, gitignored local override — lets you point GameService (e.g. NetworkShareRoot) at
// local paths without touching the committed appsettings.json/appsettings.Development.json.
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

builder.Services.Configure<GameServiceOptions>(builder.Configuration.GetSection("GameService"));

builder.Services.AddScoped<IGameService, GameService>();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddCheck("media_directory", new MediaDirectoryHealthCheck(
        builder.Configuration["GameService:GamePath"] ?? string.Empty));
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

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
app.MapGet("api/v1/games", async (IGameService service, string? platform, int? year, string? genre) =>
{
    var result = await service.GetAllAsync(platform, year, genre);
    
    return Results.Ok(result);
}).WithName("GetGames");

app.MapGet("api/v1/games/{id}", async (IGameService service, string id) =>
{
    var game = await service.GetAsync(id);

    if (game is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(game);
}).WithName("GetGame").AddEndpointFilter(EndpointFilters.RequireRouteValue("id", "Game id is required."));

app.MapGet("api/v1/games/{id}/launch", async (IGameService service, string id) =>
{
    var game = await service.GetAsync(id);
    if (game is null)
        return Results.NotFound();

    var launchIntent = await service.GetLaunchIntentAsync(id);
    if (launchIntent is null)
        return Results.Problem(
            "No network share path is configured for this game, so it cannot be launched remotely.",
            statusCode: StatusCodes.Status409Conflict);

    return Results.Ok(launchIntent);
}).WithName("LaunchGame").AddEndpointFilter(EndpointFilters.RequireRouteValue("id", "Game id is required."));

app.MapGet("api/v1/games/{id}/thumbnail", async (IGameService service, string id) =>
{
    var stream = await service.OpenThumbnailReadAsync(id);

    if (stream is null)
        return Results.NotFound();

    var contentType = stream is FileStream fs ? ThumbnailFormat.GetContentType(fs.Name) : "image/png";

    return Results.File(stream, contentType);
}).WithName("GetGameThumbnail").AddEndpointFilter(EndpointFilters.RequireRouteValue("id", "Game id is required."));


app.Run();