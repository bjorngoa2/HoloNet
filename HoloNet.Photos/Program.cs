using HoloNet.Photos.Configuration;
using HoloNet.Photos.Services;
using HoloNet.Shared.Filters;
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
    var photoMetadata = await service.GetAsync(id);

    if (photoMetadata is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(photoMetadata);
}).WithName("GetPhoto").AddEndpointFilter(EndpointFilters.RequireRouteValue("id", "Photo id is required."));

app.MapGet("api/v1/photos/{id}/image", async (IPhotoService service, string id) =>
{
    var stream = await service.OpenReadAsync(id);

    if (stream is null)
        return Results.NotFound();

    var contentType = stream is FileStream fs ? PhotoContentTypes.GetContentType(fs.Name) : "image/png";

    return Results.File(stream, contentType);
}).WithName("GetPhotoImage").AddEndpointFilter(EndpointFilters.RequireRouteValue("id", "Photo id is required."));


app.Run();
