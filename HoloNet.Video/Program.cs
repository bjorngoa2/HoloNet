using HoloNet.Shared.HealthChecks;
using HoloNet.Video.Configuration;
using HoloNet.Video.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<VideoServiceOptions>(builder.Configuration.GetSection("VideoService"));

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

app.MapHealthChecks("api/v1/health");

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
}).WithName("GetVideo").AddEndpointFilter<RequireVideoIdFilter>();

app.MapGet("api/v1/videos/{id}/stream", async (string id, IVideoService service) =>
{
    var video = await service.GetStreamAsync(id);

    if (video is null)
    {
        return Results.NotFound();
    }

    return Results.File(video.Stream, VideoFileTypes.GetContentType(video.Extension), enableRangeProcessing: true);

}).WithName("GetVideoStream").AddEndpointFilter<RequireVideoIdFilter>();

app.Run();

/// <summary>
/// Rejects a request with a 400 Problem response if its <c>{id}</c> route parameter is
/// missing/whitespace, before the endpoint handler runs. Consolidates the same guard that used
/// to be copy-pasted at the top of every <c>{id}</c>-based endpoint above. Reads the id via the
/// route value (rather than a fixed argument index) since <c>GetVideo</c> and
/// <c>GetVideoStream</c> declare their <c>id</c> parameter in different positions.
/// </summary>
internal sealed class RequireVideoIdFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var id = context.HttpContext.Request.RouteValues["id"] as string;
        if (string.IsNullOrWhiteSpace(id))
            return ValueTask.FromResult<object?>(
                Results.Problem("Video id is required.", statusCode: StatusCodes.Status400BadRequest));

        return next(context);
    }
}