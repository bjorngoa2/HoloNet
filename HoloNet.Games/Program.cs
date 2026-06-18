using HoloNet.Games.Configuration;
using HoloNet.Games.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GameServiceOptions>(builder.Configuration.GetSection("GameService"));

builder.Services.AddScoped<IGameService, GameService>();


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
app.MapGet("api/v1/games", async (IGameService service) =>
{
    var result = await service.GetAllAsync();
    
    return Results.Ok(result);
});

app.MapGet("api/v1/games/{id}", async (IGameService service, string id) =>
{
    var photoMetadata = await service.GetAsync(id);

    if (photoMetadata is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(photoMetadata);
});

app.MapGet("api/v1/games/{id}/game", async (IGameService service, string id) =>
{
    var stream = await service.OpenReadAsync(id);

    if (stream is null)
    {
        return Results.NotFound();
    }

    return Results.File(stream, "image/png");
});


app.Run();