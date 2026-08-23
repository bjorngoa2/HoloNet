using HoloNet.Shared.Filters;
using HoloNet.Shared.HealthChecks;
using HoloNet.Shared.Helpers;
using HoloNet.Video.Configuration;
using HoloNet.Video.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<VideoServiceOptions>()
    .Bind(builder.Configuration.GetSection("VideoService"))
    .Validate(o => OptionsValidation.IsValid(() => o.GetVideoDirectory()), "VideoService:VideoPath must point to an existing directory.")
    .Validate(o => OptionsValidation.IsValid(() => o.GetBaseUrl()), "VideoService:BaseUrl must be a valid absolute http/https URL.")
    .ValidateOnStart();

builder.Services.AddScoped<IVideoService, VideoService>();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddCheck("media_directory", new MediaDirectoryHealthCheck(
        builder.Configuration["VideoService:VideoPath"] ?? string.Empty));
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

app.Run();