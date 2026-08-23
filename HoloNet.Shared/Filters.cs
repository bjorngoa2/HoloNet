using Microsoft.AspNetCore.Http;

namespace HoloNet.Shared.Filters;

/// <summary>
/// Factory for Minimal API endpoint filters shared across HoloNet.Games, HoloNet.Video, and
/// HoloNet.Photos, which each used to hand-roll a nearly identical <see cref="IEndpointFilter"/>
/// class for their own "{id} is required" guard (differing only in the entity-name string in the
/// error message, and — after drifting apart — in how the id was even read: some by fixed
/// argument index, some by route value). Centralizing it here means every consumer gets the
/// same, order-independent behavior for free.
/// </summary>
public static class EndpointFilters
{
    /// <summary>
    /// Rejects a request with a 400 <see cref="ProblemDetails"/> response if its
    /// <paramref name="routeParameterName"/> route value is missing/whitespace, before the
    /// endpoint handler runs. Reads the value via route value (not a fixed argument index), so
    /// it doesn't matter where in an endpoint's parameter list the route parameter is declared.
    /// </summary>
    public static Func<EndpointFilterInvocationContext, EndpointFilterDelegate, ValueTask<object?>> RequireRouteValue(
        string routeParameterName, string missingValueMessage) =>
        (context, next) =>
        {
            var value = context.HttpContext.Request.RouteValues[routeParameterName] as string;
            return string.IsNullOrWhiteSpace(value)
                ? ValueTask.FromResult<object?>(Results.Problem(missingValueMessage, statusCode: StatusCodes.Status400BadRequest))
                : next(context);
        };
}
