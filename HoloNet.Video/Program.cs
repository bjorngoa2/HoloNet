using HoloNet.Video.Configuration;
using HoloNet.Video.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<VideoServiceOptions>(builder.Configuration.GetSection("VideoService"));

builder.Services.AddScoped<IVideoService, VideoService>();

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

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
}).WithName("GetVideo");

app.MapGet("api/v1/videos/{id}/stream", async (string id, IVideoService service) =>
{
    var stream = await service.GetStreamAsync(id);

    if (stream == null)
    {
        return Results.NotFound();
    }

    var video = await service.GetAsync(id);
    var contentType = video?.Extension.ToLowerInvariant() switch
    {
        ".mp4" => "video/mp4",
        ".mkv" => "video/x-matroska",
        ".avi" => "video/x-msvideo",
        ".mov" => "video/quicktime",
        _ => "application/octet-stream"
    };

    return Results.File(stream, contentType, enableRangeProcessing: true);
    
}).WithName("GetVideoStream");

app.Run();