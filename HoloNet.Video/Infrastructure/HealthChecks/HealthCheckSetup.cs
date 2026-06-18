namespace HoloNet.Video.Infrastructure.HealthChecks;


public static class HealthCheckSetup
{
    public static IServiceCollection RegisterHealthChecks(this IServiceCollection services,
        IConfiguration configuration)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapHealthChecks(this IEndpointRouteBuilder app)
    {
        app.MapGet("health", () => Results.Ok(new
        {
            status = "healthy", service = "HoloNet.Video"
        }));

        return app;
    }
}