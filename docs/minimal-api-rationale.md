# Why HoloNet Uses Minimal APIs

## What Are Minimal APIs?

ASP.NET Core offers two ways to build HTTP APIs:

- **Controller-based APIs** — the classic MVC model. Endpoints are methods on classes
  decorated with `[ApiController]`, `[HttpGet]`, etc. The framework scans assemblies at
  startup, builds action descriptors, and routes requests through a filter pipeline.

- **Minimal APIs** — introduced in .NET 6, now the default in .NET 8+. Endpoints are
  lambda functions registered directly on the `WebApplication` instance. No controllers,
  no attributes, no reflection scanning.

HoloNet uses Minimal APIs throughout. Here is why.

---

## Why HoloNet Uses Minimal APIs

### 1. Each service has a small, focused surface

Every HoloNet service exposes exactly three business endpoints: list all, get by id, and
serve the file. That surface fits naturally in a single `Program.cs` with no organisational
overhead.

**HoloNet (Minimal API) — the entire endpoint surface for the Video service:**

```csharp
app.MapGet("api/v1/videos", async (IVideoService service) =>
{
    var result = await service.GetAllAsync();
    return Results.Ok(result);
}).WithName("GetVideos").WithOpenApi();

app.MapGet("api/v1/videos/{id}", async (IVideoService service, string id) =>
{
    var video = await service.GetAsync(id);
    return video is null ? Results.NotFound() : Results.Ok(video);
}).WithName("GetVideo").WithOpenApi();

app.MapGet("api/v1/videos/{id}/stream", async (string id, IVideoService service) =>
{
    var stream = await service.GetStreamAsync(id);
    return stream is null
        ? Results.NotFound()
        : Results.File(stream, "video/mp4", enableRangeProcessing: true);
}).WithName("GetVideoStream").WithOpenApi();
```

**Equivalent controller — same three endpoints, significantly more ceremony:**

```csharp
[ApiController]
[Route("api/v1/videos")]
public class VideosController(IVideoService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await service.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var video = await service.GetAsync(id);
        return video is null ? NotFound() : Ok(video);
    }

    [HttpGet("{id}/stream")]
    public async Task<IActionResult> Stream(string id)
    {
        var stream = await service.GetStreamAsync(id);
        if (stream is null) return NotFound();
        Response.Headers.AcceptRanges = "bytes";
        return File(stream, "video/mp4", enableRangeProcessing: true);
    }
}
```

The controller version requires a new file, a class, a base class, and attribute decoration
— for the exact same three operations.

---

### 2. No MVC middleware overhead

Controller-based APIs depend on the full MVC middleware pipeline, which at startup:

- Scans all assemblies via reflection to discover controller types
- Builds an action descriptor collection (one entry per endpoint)
- Constructs a route table from `[Route]` and `[Http*]` attributes
- Wires up the action filter pipeline (`IActionFilter`, `IResultFilter`, etc.)

Minimal APIs skip all of this. Endpoints are registered directly as route handlers — there
is no discovery phase, no descriptor collection, and no filter pipeline unless you
explicitly add one.

For a service with 3 endpoints running in a Docker container, this is not a theoretical
concern — it is unnecessary complexity added by default.

---

### 3. Faster cold start and smaller container footprint

The absence of MVC assembly scanning and descriptor building means containers start
faster. On a homelab server where containers may restart due to updates, power cycles,
or `docker compose up`, this matters in practice.

Minimal APIs also require fewer NuGet packages. You do not need
`Microsoft.AspNetCore.Mvc.*` — the base `Microsoft.AspNetCore.App` framework reference
covers everything HoloNet needs.

---

### 4. Idiomatic .NET 8+

Since .NET 6, `dotnet new webapi` generates a Minimal API project by default. The
controller template is still available but is no longer the recommended starting point
for new APIs. Minimal APIs are where Microsoft is investing in documentation, performance
improvements, and new features (e.g. typed results, endpoint filters, `IEndpointFilter`).

Writing HoloNet in the current idiomatic style means less friction when consulting
official docs and examples.

---

### 5. Matches the project scope

HoloNet is a personal, LAN-only, single-developer homelab platform. Controllers were
designed to solve problems that arise at a different scale:

- Organising dozens or hundreds of endpoints across a large team
- Sharing action filters, auth policies, and model validation across many controllers
- Supporting complex model binding scenarios

None of these apply here. Adding the controller model would introduce indirection and
boilerplate that buys nothing for this project.

---

## When You Would Switch to Controllers

Minimal APIs are the right choice for HoloNet, but there are real scenarios where
controllers are the better fit:

| Scenario | Why controllers help |
|---|---|
| 20+ endpoints per service | Controllers let you split endpoints across files by resource type without losing cohesion |
| Shared action filters | `[ServiceFilter]`, `[TypeFilter]`, and global filters are well-supported on controllers; Minimal API equivalents (`IEndpointFilter`) are newer and less familiar |
| Complex model binding | `[FromBody]`, `[FromForm]`, `[FromHeader]` with validation attributes are more mature in the MVC pipeline |
| Large team | The class-per-resource structure of controllers is a familiar convention many .NET developers know by default |
| Existing MVC codebase | Mixing both styles is possible but adds cognitive overhead; if the project already uses controllers, stay consistent |

The honest summary: if a service in this project grew to 15+ endpoints with shared auth
filters across all of them, the balance would tip toward controllers.

---

## Further Reading

- [Minimal APIs overview — Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/overview)
- [Choose between controller-based and Minimal APIs — Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/tutorials/choose-web-ui)
- [Minimal APIs vs. controller-based APIs — Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis)
