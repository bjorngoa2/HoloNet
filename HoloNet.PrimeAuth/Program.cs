using System.Net.Http.Json;
using Microsoft.Playwright;

// HoloNet.PrimeAuth: a small standalone tool that opens a real, visible browser window so you
// can log into Prime Video normally (handling CAPTCHA/2FA yourself, exactly as you would in your
// everyday browser), then captures the resulting session cookie and pushes it to a running
// HoloNet.Video instance via its admin endpoint - see HoloNet.Video/Infrastructure/
// PrimeVideoSessionStore.cs. This exists because HoloNet.Video itself runs headless in a Docker
// container on the home server and can never show a login window; this tool is meant to be run
// on a machine with a display (the TV PC, or your own PC) whenever the previously captured
// cookie has expired.
//
// Deliberately does NOT store your Amazon password anywhere, does NOT attempt to script/automate
// the login form itself, and never touches Prime's playback/DRM endpoints - it only reads
// whatever cookies your own browser session already holds after YOU log in, then relays them to
// HoloNet.Video, the same as manually copy-pasting a Cookie header out of DevTools would.

if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.WriteLine("Usage: HoloNet.PrimeAuth <HoloNet.Video base URL>");
    Console.WriteLine("Example: HoloNet.PrimeAuth http://videos.goa.no");
    Console.WriteLine("         HoloNet.PrimeAuth http://localhost:5044");
    return 1;
}

var videoBaseUrl = args[0].TrimEnd('/');

Console.WriteLine("Launching a browser window - please log into Prime Video there.");
Console.WriteLine("(Solve any CAPTCHA/2FA prompt yourself, same as normal.)");
Console.WriteLine();

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false,
});

var context = await browser.NewContextAsync();
var page = await context.NewPageAsync();
await page.GotoAsync("https://www.primevideo.com");

Console.WriteLine("Once you can see your Prime Video home page (i.e. you're fully logged in),");
Console.WriteLine("come back here and press Enter.");
Console.ReadLine();

// CookiesAsync(urls) returns only the cookies that would actually be sent for a request to that
// URL (respecting domain/path/secure matching) - i.e. exactly the Cookie header value a real
// browser request to primevideo.com would carry, same shape as what was previously captured by
// hand via DevTools.
var cookies = await context.CookiesAsync(["https://www.primevideo.com"]);
if (cookies.Count == 0)
{
    Console.WriteLine();
    Console.WriteLine("No cookies were found for primevideo.com - it doesn't look like you're logged in. Nothing was sent.");
    return 1;
}

var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));

await browser.CloseAsync();

Console.WriteLine();
Console.WriteLine($"Captured {cookies.Count} cookie(s). Sending to {videoBaseUrl}...");

using var httpClient = new HttpClient();
try
{
    var response = await httpClient.PutAsJsonAsync(
        $"{videoBaseUrl}/api/v1/admin/primevideo/session-cookie",
        new { Cookie = cookieHeader });

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"HoloNet.Video rejected the update: {(int)response.StatusCode} {response.StatusCode}");
        return 1;
    }
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Could not reach HoloNet.Video at {videoBaseUrl}: {ex.Message}");
    return 1;
}

Console.WriteLine("Done - HoloNet.Video's Prime Video session has been refreshed.");
return 0;
