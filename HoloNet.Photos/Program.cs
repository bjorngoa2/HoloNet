using HoloNet.Photos.Configuration;
using HoloNet.Photos.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<PhotoServiceOptions>(builder.Configuration.GetSection("PhotoService"));

builder.Services.AddScoped<IPhotoService, PhotoService>();


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHealthChecks();
var app = builder.Build();
app.MapHealthChecks("api/v1/health");   
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


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

    return Results.File(stream, "image/png");
}).WithName("GetPhotoImage");


app.Run();
