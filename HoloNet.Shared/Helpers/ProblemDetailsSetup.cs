using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HoloNet.Shared.Helpers;

public static class ProblemDetailsSetup
{
    /// <summary>
    /// Registers <see cref="ProblemDetails"/> error responses for <c>UseExceptionHandler()</c>
    /// and <c>UseStatusCodePages()</c>. A request ASP.NET Core can't bind (e.g. malformed JSON)
    /// keeps its own 4xx status rather than becoming a 500: in Development, minimal APIs throw
    /// <see cref="BadHttpRequestException"/> for these instead of answering 400 themselves.
    /// </summary>
    public static IServiceCollection AddHoloNetProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.Configure<ExceptionHandlerOptions>(options => options.StatusCodeSelector = exception =>
            exception is BadHttpRequestException badRequest ? badRequest.StatusCode : StatusCodes.Status500InternalServerError);
        return services;
    }
}