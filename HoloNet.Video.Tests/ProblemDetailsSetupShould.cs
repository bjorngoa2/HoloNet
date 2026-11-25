using HoloNet.Shared.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HoloNet.Video.Tests;

/// <summary>
/// Verifies <see cref="ProblemDetailsSetup"/> keeps a request ASP.NET Core couldn't bind as a
/// client error rather than reporting it as a server fault.
/// </summary>
public class ProblemDetailsSetupShould
{
    [Fact]
    public void KeepTheStatusOfRequestsThatCouldNotBeBound()
    {
        var selector = StatusCodeSelector();

        Assert.Equal(StatusCodes.Status400BadRequest, selector(new BadHttpRequestException("Malformed JSON")));
        Assert.Equal(StatusCodes.Status415UnsupportedMediaType, selector(new BadHttpRequestException("Not JSON", StatusCodes.Status415UnsupportedMediaType)));
    }

    [Fact]
    public void ReportAnyOtherException_AsAServerError() =>
        Assert.Equal(StatusCodes.Status500InternalServerError, StatusCodeSelector()(new InvalidOperationException()));

    private static Func<Exception, int> StatusCodeSelector()
    {
        using var provider = new ServiceCollection().AddHoloNetProblemDetails().BuildServiceProvider();
        var selector = provider.GetRequiredService<IOptions<ExceptionHandlerOptions>>().Value.StatusCodeSelector;
        return Assert.IsType<Func<Exception, int>>(selector);
    }
}