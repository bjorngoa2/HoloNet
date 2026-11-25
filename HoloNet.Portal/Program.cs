using HoloNet.Portal.Configuration;
using HoloNet.Shared.Helpers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<PortalOptions>(builder.Configuration.GetSection("Portal"));
builder.Services.AddOpenApi();
builder.Services.AddHoloNetProblemDetails();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapHealthChecks("api/v1/health").WithName("HealthCheck");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("api/v1/config", (Microsoft.Extensions.Options.IOptions<PortalOptions> opts) =>
    Results.Ok(opts.Value)).WithName("GetConfig");

app.MapFallbackToFile("index.html");

app.Run();
