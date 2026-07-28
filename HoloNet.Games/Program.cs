using HoloNet.Games.Configuration;
using HoloNet.Games.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GameServiceOptions>(builder.Configuration.GetSection("GameService"));

builder.Services.AddScoped<IGameService, GameService>();

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.MapHealthChecks("api/v1/health").WithName("HealthCheck");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();


// Endpoints
app.MapGet("api/v1/games", async (IGameService service) =>
{
    var result = await service.GetAllAsync();
    
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
}).WithName("GetGame");


app.Run();