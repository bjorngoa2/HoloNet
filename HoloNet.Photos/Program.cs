using HoloNet.Photos.Configuration;
using HoloNet.Photos.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<PhotoServiceOptions>(builder.Configuration.GetSection("PhotoService"));

builder.Services.AddScoped<IPhotoService, PhotoService>();

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
app.MapGet("api/v1/photos", async (IPhotoService service) =>
{
    var result = await service.GetAllAsync();
    
    return Results.Ok(result);
}).WithName("GetPhotos");

app.MapGet("api/v1/photos/{id}", async (IPhotoService service, string id) =>
{
    var photoMetadata = await service.GetAsync(id);

    if (photoMetadata is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(photoMetadata);
}).WithName("GetPhoto");

app.MapGet("api/v1/photos/{id}/image", async (IPhotoService service, string id) =>
{
    var stream = await service.OpenReadAsync(id);

    if (stream is null)
    {
        return Results.NotFound();
    }

    var photo = await service.GetAsync(id);
    var contentType = photo?.Extension.ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };

    return Results.File(stream, contentType);
}).WithName("GetPhotoImage");


app.Run();
