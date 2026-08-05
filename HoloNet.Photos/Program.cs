using HoloNet.Photos.Configuration;
using HoloNet.Photos.Services;
using HoloNet.Shared.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<PhotoServiceOptions>(builder.Configuration.GetSection("PhotoService"));

builder.Services.AddScoped<IPhotoService, PhotoService>();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddCheck("media_directory", new MediaDirectoryHealthCheck(
        builder.Configuration["PhotoService:PhotoPath"] ?? string.Empty));
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
app.MapGet("api/v1/photos", async (IPhotoService service) =>
{
    var result = await service.GetAllAsync();
    
    return Results.Ok(result);
}).WithName("GetPhotos");

app.MapGet("api/v1/photos/{id}", async (IPhotoService service, string id) =>
{
    if (string.IsNullOrWhiteSpace(id))
        return Results.Problem("Photo id is required.", statusCode: StatusCodes.Status400BadRequest);

    var photoMetadata = await service.GetAsync(id);

    if (photoMetadata is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(photoMetadata);
}).WithName("GetPhoto");

app.MapGet("api/v1/photos/{id}/image", async (IPhotoService service, string id) =>
{
    if (string.IsNullOrWhiteSpace(id))
        return Results.Problem("Photo id is required.", statusCode: StatusCodes.Status400BadRequest);

    var stream = await service.OpenReadAsync(id);

    if (stream is null)
        return Results.NotFound();

    var contentType = stream is FileStream fs
        ? Path.GetExtension(fs.Name).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif"            => "image/gif",
            ".webp"           => "image/webp",
            ".bmp"            => "image/bmp",
            _                 => "image/png"
        }
        : "image/png";

    return Results.File(stream, contentType);
}).WithName("GetPhotoImage");


app.Run();
