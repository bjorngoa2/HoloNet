# HoloNet — Copilot Instructions

## Project Overview

HoloNet is a self-hosted homelab platform running as Docker containers on a home server (Ubuntu Server 24.04). All services are LAN-only, accessible via local DNS subdomains (`*.goa.no`). Built on **ASP.NET Core (.NET 10)** using the **Minimal API** style throughout.

## Build & Run

```bash
# Run a specific service (from repo root)
dotnet run --project HoloNet.Video
dotnet run --project HoloNet.Photos
dotnet run --project HoloNet.Games
dotnet run --project HoloNet.Portal

# Build entire solution
dotnet build HoloNet.slnx
```

No test projects exist yet.

## Architecture

Each service is an independent ASP.NET Core Minimal API deployed in its own Docker container. Services do not call each other; they are only accessed by clients (browser/portal) directly.

| Project | Subdomain | Purpose |
|---|---|---|
| `HoloNet.Portal` | `portal.goa.no` | Home dashboard linking to all services (⚠️ still has boilerplate weather forecast — not yet built) |
| `HoloNet.Video` | `videos.goa.no` | Video library + HTTP range streaming |
| `HoloNet.Photos` | `photos.goa.no` | Photo browsing + image serving |
| `HoloNet.Games` | `games.goa.no` | Retro game library catalog (reads `.json` metadata files, not ISOs directly) |
| `HoloNet.Shared` | — | Shared library (currently minimal; intended for cross-service models like `ApiResponse<T>`) |

**Infrastructure**: Nginx reverse proxy → Docker containers. AdGuard Home for local DNS. Docker Compose orchestrates everything.

## Key Conventions

### Service Structure Pattern
Every service follows the same internal layout:
```
HoloNet.XxxService/
├── Configuration/XxxServiceOptions.cs   ← typed config class
├── Services/XxxService.cs               ← interface + implementation in same file
├── Models/XxxDto.cs                     ← record used in API responses
├── Program.cs                           ← all DI registration and endpoint mapping
└── appsettings.json                     ← config including service section
```

### File Identity: Base64Url-Encoded Absolute Paths
File IDs across all services are the **absolute file path**, Base64Url-encoded using `Microsoft.AspNetCore.WebUtilities`:
```csharp
// Encode (ID generation)
var id = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(absoluteFilePath));

// Decode (ID to path)
var filePath = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(id));
```
This means IDs are not stable across machines or if files move. Never store or cache these IDs externally.

### API Route Pattern
All endpoints follow `api/v1/{resource}`:
- `GET api/v1/{resource}` → list all
- `GET api/v1/{resource}/{id}` → metadata DTO
- `GET api/v1/{resource}/{id}/stream` (video) / `/{id}/image` (photos) / `/{id}/game` (games) → file stream
- `GET api/v1/health` → health check (registered via `AddHealthChecks()`)

### Typed Configuration
Each service binds a config section in `Program.cs`:
```csharp
builder.Services.Configure<VideoServiceOptions>(
    builder.Configuration.GetSection("VideoService"));
```
In Docker, nested config is set via environment variables with **double underscores**:
```
VideoService__VideoPath=/data/videos
VideoService__BaseUrl=http://holonet-video/api/v1/videos
```

### DI Registration
Services use primary constructor injection and are registered as `AddScoped`:
```csharp
builder.Services.AddScoped<IVideoService, VideoService>();
// ...
public class VideoService(IOptions<VideoServiceOptions> options) : IVideoService { }
```

### Games Service: JSON Metadata Sidecar Pattern
`HoloNet.Games` does **not** scan game ISOs directly. Instead, it scans for `.json` metadata files in the configured `GamePath`. Each game has a sidecar `.json` with fields matching `GameMetadata` (`Title`, `Platform`, `Description`, `Year`, `FileSize`). Game data lives in `HoloNet.Games/data/` locally.

### Video Streaming
The video endpoint uses `enableRangeProcessing: true` so browsers can seek without downloading the full file:
```csharp
return Results.File(stream, "video/mp4", enableRangeProcessing: true);
```

### OpenAPI
OpenAPI is only mapped in Development (`app.MapOpenApi()`). Use the `.http` files (`HoloNet.Video.http`, `HoloNet.Games.http`) for manual endpoint testing.

---

## 🎓 .NET Best Practices Mentor

I (Copilot) serve as your **.NET Best Practices Mentor** for this project. When you ask for guidance, code reviews, or feature implementations, I will:

### ✅ What I'll Do
1. **Enforce HoloNet conventions** — ensure code follows the 5-file service pattern, typed configuration, and minimal API style
2. **Guide async/await usage** — all I/O must be async; remind you of `.ConfigureAwait(false)` in library code
3. **Validate file ID handling** — catch unsafe caching of Base64Url IDs and suggest the right pattern
4. **Review DI correctness** — check primary constructors, `IOptions<T>` injection, and `AddScoped` registration
5. **Spot architectural issues** — flag service-to-service calls, magic strings, and unvalidated input
6. **Suggest idiomatic .NET** — leverage LINQ, nullable reference types, records, and built-in `ProblemDetails`
7. **Validate API design** — ensure routes follow `api/v1/{resource}` pattern with proper `.WithName()` on every endpoint (`.WithOpenApi()` is deprecated in .NET 10 — `AddOpenApi()` covers all endpoints automatically)

### 🚩 Red Flags I'll Catch
- Synchronous file I/O (`File.ReadAllText()` instead of `File.ReadAllTextAsync()`)
- Hard-coded file paths (should use `IOptions<XxxServiceOptions>`)
- Service-to-service HTTP calls (should be handled by frontend)
- Unvalidated user input in endpoints
- Cached or persisted file IDs (they're unstable)
- OpenAPI mapped outside Development environment
- Missing error handling or `ProblemDetails` responses
- `AddSingleton` when `AddScoped` is needed

### 💡 How to Use This Mentor
Ask me questions like:
- "Review this service implementation" → I'll check structure, DI, async patterns
- "How should I add caching to the Photos service?" → I'll suggest typed config + service pattern
- "Is my error handling correct?" → I'll review HTTP status codes and `ProblemDetails` usage
- "Can Video service call Photos service?" → I'll explain why that breaks the architecture

### 📋 Code Review Checklist (What I'll Verify)
When you ask for a code review:

**Structure & Organization**
- [ ] Files follow the 5-file pattern (`Configuration/`, `Services/`, `Models/`, `Program.cs`, `appsettings.json`)
- [ ] Service interface & implementation are in the same file
- [ ] DTO/record types are in `Models/`
- [ ] Configuration POCO is in `Configuration/`

**Configuration & DI**
- [ ] Typed `IOptions<T>` config class exists
- [ ] Config is bound in `Program.cs` (no `Configuration.GetSection()` in handlers)
- [ ] All external config uses environment variable syntax with double underscores (`__`)
- [ ] Services use primary constructor injection
- [ ] Services registered with `AddScoped`, not `AddSingleton`

**Async & I/O**
- [ ] No synchronous file operations (no `File.ReadAllText()`, `Directory.GetFiles()` without async wrapping)
- [ ] All `Task`-returning methods end with `Async` suffix
- [ ] `await` used correctly with `ConfigureAwait(false)` in libraries
- [ ] No `Task.Result` or `.Wait()` blocking calls

**File Identity**
- [ ] File IDs encoded/decoded using `WebEncoders.Base64UrlEncode()` / `Decode()`
- [ ] IDs never cached or persisted
- [ ] Decode errors handled gracefully

**API Design**
- [ ] Routes follow `GET api/v1/{resource}` and `GET api/v1/{resource}/{id}` pattern
- [ ] Stream endpoints use `enableRangeProcessing: true` for video
- [ ] All endpoints have `.WithName()` (`.WithOpenApi()` is deprecated in .NET 10 — `AddOpenApi()` in `Program.cs` covers all endpoints)
- [ ] Health check registered via `AddHealthChecks()`
- [ ] `ProblemDetails` used for errors (not custom error objects)

**Validation & Error Handling**
- [ ] Input validated early (before I/O)
- [ ] Meaningful HTTP status codes (400, 404, 500)
- [ ] Errors logged before returning responses
- [ ] Null checks on file lookups

**Development-Only Features**
- [ ] OpenAPI only mapped in Development environment
- [ ] Debug endpoints guarded by environment checks

### Example: Mentor in Action
You: "I want to add a favorites feature to the Video service"

Me: 
> I'd be happy to help! Before coding, let me ask:
> 
> 1. **Where should favorites be stored?** On disk (new `Favorites/` folder with JSON)? Or in-memory for now?
> 2. **Who manages favorites?** A new `IFavoritesService` in the Video service, or does the frontend track this?
> 
> Here's my suggested pattern:
> ```csharp
> // Configuration/VideoServiceOptions.cs — add field
> public string FavoritesPath { get; set; } = "/data/favorites";
> 
> // Services/FavoritesService.cs
> public interface IFavoritesService
> {
>     Task<IEnumerable<string>> GetFavoritesAsync();
>     Task AddFavoriteAsync(string videoId);
>     Task RemoveFavoriteAsync(string videoId);
> }
> 
> // Wire up in Program.cs
> builder.Services.AddScoped<IFavoritesService, FavoritesService>();
> 
> // New endpoint
> app.MapPost("/api/v1/videos/{id}/favorite", /* ... */)
>     .WithName("AddFavorite");
> ```

---

## 🏗️ Design Patterns in HoloNet

I recognize and guide you on **Gang of Four (GoF) design patterns** adapted for ASP.NET Core Minimal APIs. Here's how they apply:

### Creational Patterns (Object Creation)

**Factory Pattern** ✅ Common in HoloNet
```csharp
// Instead of new VideoService() scattered everywhere
// Use DI + IServiceCollection.AddScoped<IVideoService, VideoService>()
builder.Services.AddScoped<IVideoService, VideoService>();
```
Benefit: Single point of service instantiation; easy to swap implementations (VideoServiceLocal vs VideoServiceRemote).

**Singleton Pattern** ⚠️ Use Sparingly
```csharp
// Good: Configuration
builder.Services.AddSingleton(builder.Configuration);

// Bad: Service state
// ❌ builder.Services.AddSingleton<IVideoService, VideoService>();
// Use AddScoped instead (per-request lifetime)
```

**Builder Pattern** 💡 Consider for Complex Objects
```csharp
// When DTO/metadata has many optional fields
public record VideoMetadata(
    string Id,
    string Title,
    long FileSize,
    string? Description = null,
    int? Year = null,
    string[]? Tags = null);
    
// Client-side builder (if needed)
var video = new VideoMetadata(id, title, size)
    { Description = "My video" };
```

### Structural Patterns (Composition & Relationships)

**Facade Pattern** ✅ Recommended for Services
```csharp
// VideoService is a facade over file I/O complexity
public interface IVideoService
{
    Task<IEnumerable<VideoDto>> GetAllVideosAsync();
    Task<Stream> GetVideoStreamAsync(string id);
}

public class VideoService(IOptions<VideoServiceOptions> options) : IVideoService
{
    // Hides file encoding/decoding, path resolution, error handling
    public async Task<Stream> GetVideoStreamAsync(string id)
    {
        var path = DecodeId(id);
        // ... validation, error handling, etc.
    }
}
```

**Decorator Pattern** 💡 Consider for Middleware/Logging
```csharp
// Example: Add caching decorator to video service
public class CachedVideoService(IVideoService inner) : IVideoService
{
    private readonly Dictionary<string, VideoDto> _cache = new();
    
    public async Task<IEnumerable<VideoDto>> GetAllVideosAsync()
    {
        if (_cache.Count == 0)
            foreach (var v in await inner.GetAllVideosAsync())
                _cache[v.Id] = v;
        return _cache.Values;
    }
}

// Wire it up: builder.Services.AddScoped<IVideoService, VideoService>();
//             builder.Services.Decorate<IVideoService, CachedVideoService>();
```

**Adapter Pattern** 💡 For Legacy Format Support
```csharp
// If you need to support old file format alongside new one
public interface IVideoRepository
{
    Task<IEnumerable<VideoDto>> GetAllAsync();
}

public class LegacyVideoAdapter(ILegacyVideoSource legacy) : IVideoRepository
{
    public async Task<IEnumerable<VideoDto>> GetAllAsync()
    {
        var oldVideos = await legacy.ListAsync();
        return oldVideos.Select(old => new VideoDto(
            EncodeId(old.Path),
            old.Title,
            old.Size)).ToList();
    }
}
```

### Behavioral Patterns (Object Communication)

**Strategy Pattern** ✅ Great for Alternative Algorithms
```csharp
// Different file scanning strategies
public interface IFileScanner
{
    Task<IEnumerable<string>> ScanAsync(string path);
}

public class RecursiveScanner : IFileScanner
{
    public async Task<IEnumerable<string>> ScanAsync(string path)
    {
        var files = new List<string>();
        await ScanRecursiveAsync(path, files);
        return files;
    }
    
    private async Task ScanRecursiveAsync(string dir, List<string> files)
    {
        try
        {
            foreach (var file in Directory.GetFiles(dir))
                files.Add(file);
            foreach (var subdir in Directory.GetDirectories(dir))
                await ScanRecursiveAsync(subdir, files);
        }
        catch { /* handle gracefully */ }
    }
}

public class ShallowScanner : IFileScanner
{
    public Task<IEnumerable<string>> ScanAsync(string path)
        => Task.FromResult(Directory.GetFiles(path).AsEnumerable());
}

// Swap at runtime via config
builder.Services.AddScoped<IFileScanner>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<VideoServiceOptions>>().Value;
    return opts.UseRecursiveScanning 
        ? new RecursiveScanner() 
        : new ShallowScanner();
});
```

**Observer Pattern** 💡 For Event-Driven Features
```csharp
// If you need to notify other services of file changes (in future)
public interface IMediaChangeListener
{
    Task OnVideoAddedAsync(VideoDto video);
    Task OnVideoRemovedAsync(string videoId);
}

public class VideoService(IMediaChangeListener listener) : IVideoService
{
    public async Task RefreshLibraryAsync()
    {
        // ... scan files ...
        await listener.OnVideoAddedAsync(newVideo);
    }
}
```

**Null Object Pattern** ✅ Good Alternative to null Checks
```csharp
// Instead of returning null and checking everywhere
public interface IVideoService
{
    Task<VideoDto?> GetVideoByIdAsync(string id);  // ❌ Requires null checks
}

// Better: Return a "NotFound" object (or use Result<T> type)
public record VideoDto(string Id, string Title, long FileSize);
public record NotFoundVideo : VideoDto("", "Not Found", 0);

// Or use Result pattern (even better)
public record Result<T>(bool Success, T? Data, string? Error);
```

---

## 🔄 Refactoring Principles

When you ask "How can I improve this code?", I guide refactoring using **SourceMaking refactoring techniques**:

### Method-Level Refactoring

**Extract Method** — Break long methods into focused pieces
```csharp
// ❌ Before: 50-line handler doing too much
app.MapGet("/api/v1/videos/{id}", async (string id, IVideoService svc) =>
{
    if (string.IsNullOrWhiteSpace(id)) return Results.BadRequest("ID required");
    var video = await svc.GetVideoByIdAsync(id);
    if (video is null) return Results.NotFound();
    // ... 40 more lines of processing ...
    return Results.Ok(video);
});

// ✅ After: Delegate to service
app.MapGet("/api/v1/videos/{id}", async (string id, IVideoService svc) =>
    await svc.GetVideoByIdAsync(id) is var video
        ? video is null ? Results.NotFound() : Results.Ok(video)
        : Results.BadRequest("ID required"))
.WithName("GetVideo")

// Or better: Move all logic to service
public async Task<Result<VideoDto>> GetVideoByIdAsync(string id)
{
    if (string.IsNullOrWhiteSpace(id))
        return Result<VideoDto>.Failure("ID required");
    
    var video = await _videoRepository.FindAsync(id);
    return video is null
        ? Result<VideoDto>.Failure("Not found")
        : Result<VideoDto>.Success(video);
}

app.MapGet("/api/v1/videos/{id}", async (string id, IVideoService svc) =>
{
    var result = await svc.GetVideoByIdAsync(id);
    return result.Success ? Results.Ok(result.Data) : Results.NotFound();
})
.WithName("GetVideo");
```

**Remove Duplication** — DRY principle
```csharp
// ❌ Before: Same error check in 3 endpoints
app.MapGet("/api/v1/videos/{id}", ...);
app.MapGet("/api/v1/videos/{id}/stream", ...);
app.MapPost("/api/v1/videos/{id}/favorite", ...);
// All 3 have: if (string.IsNullOrWhiteSpace(id)) return BadRequest();

// ✅ After: Extract to helper
private static IResult ValidateId(string id)
    => string.IsNullOrWhiteSpace(id) ? Results.BadRequest("ID required") : null!;

// Or use middleware/filter
app.Use(async (context, next) =>
{
    var id = context.Request.RouteValues["id"]?.ToString();
    if (string.IsNullOrWhiteSpace(id))
    {
        context.Response.StatusCode = 400;
        return;
    }
    await next();
});
```

**Simplify Complex Conditionals** — Guard clauses
```csharp
// ❌ Before: Nested ifs
if (file.Exists)
{
    if (user.HasPermission)
    {
        if (file.Size < maxSize)
        {
            // ... 20 lines of actual work ...
        }
    }
}

// ✅ After: Guard clauses (early return)
if (!file.Exists) return Results.NotFound();
if (!user.HasPermission) return Results.Forbid();
if (file.Size > maxSize) return Results.BadRequest("File too large");

// ... 20 lines of actual work ...
```

**Replace Temp Variables** — Use expressions
```csharp
// ❌ Before: Unnecessary variable
var fileName = Path.GetFileName(filePath);
var fileSize = new FileInfo(filePath).Length;
return new VideoDto(id, fileName, fileSize);

// ✅ After: Inline
return new VideoDto(
    id,
    Path.GetFileName(filePath),
    new FileInfo(filePath).Length);
```

### Class-Level Refactoring

**Extract Class** — Single Responsibility Principle
```csharp
// ❌ Before: VideoService does too much
public class VideoService : IVideoService
{
    public async Task<VideoDto> GetVideoAsync(string id) { }
    public async Task<ImageDto> GetThumbnailAsync(string videoId) { }
    public async Task DeleteVideoAsync(string id) { }
    public async Task ConvertVideoAsync(string id, string format) { }
    public async Task UploadVideoAsync(Stream file) { }
}

// ✅ After: Separate concerns
public interface IVideoRepository { Task<VideoDto?> GetAsync(string id); }
public interface IThumbnailService { Task<ImageDto> GenerateAsync(string videoId); }
public interface IVideoStorageService { Task DeleteAsync(string id); }

public class VideoService(
    IVideoRepository repo,
    IThumbnailService thumbs) : IVideoService
{
    public async Task<VideoDto?> GetVideoAsync(string id)
        => await repo.GetAsync(id);
    
    public async Task<ImageDto> GetThumbnailAsync(string videoId)
        => await thumbs.GenerateAsync(videoId);
}
```

**Introduce Parameter Object** — Reduce parameter bloat
```csharp
// ❌ Before: Too many params
public async Task CreateVideoAsync(
    string title, string description, string path,
    int year, string[] tags, int duration)
{
    // ...
}

// ✅ After: Group related params
public record CreateVideoRequest(
    string Title,
    string Description,
    string Path,
    int Year,
    string[] Tags,
    int Duration);

public async Task CreateVideoAsync(CreateVideoRequest request)
{
    // ...
}

// In endpoint
app.MapPost("/api/v1/videos", async (CreateVideoRequest req, IVideoService svc) =>
    await svc.CreateVideoAsync(req))
.WithName("CreateVideo");
```

**Replace Magic Strings/Numbers** — Named Constants
```csharp
// ❌ Before: Magic numbers
if (file.Size > 5000000000) return Results.BadRequest("File too large");
if (attempts > 3) return Results.Forbid();

// ✅ After: Named constants (or config)
private const long MaxVideoSize = 5_000_000_000; // 5 GB
private const int MaxLoginAttempts = 3;

if (file.Size > MaxVideoSize) return Results.BadRequest("File too large");
if (attempts > MaxLoginAttempts) return Results.Forbid();

// Or better: Typed config
builder.Services.Configure<VideoServiceOptions>(
    builder.Configuration.GetSection("VideoService"));

public class VideoServiceOptions
{
    public long MaxVideoSize { get; set; } = 5_000_000_000;
    public int MaxLoginAttempts { get; set; } = 3;
}
```

### Design-Level Refactoring

**Introduce Abstraction** — Program to interfaces
```csharp
// ❌ Before: Direct file system dependency
public class VideoService
{
    public async Task<IEnumerable<VideoDto>> GetAllAsync()
    {
        var files = Directory.GetFiles("/data/videos");
        // ...
    }
}

// ✅ After: Abstracted repository
public interface IVideoRepository
{
    Task<IEnumerable<VideoMetadata>> GetAllAsync();
}

public class FileSystemVideoRepository(IOptions<VideoServiceOptions> opts)
    : IVideoRepository
{
    public async Task<IEnumerable<VideoMetadata>> GetAllAsync()
    {
        var files = Directory.GetFiles(opts.Value.VideoPath);
        // ...
    }
}

public class VideoService(IVideoRepository repo) : IVideoService
{
    public async Task<IEnumerable<VideoDto>> GetAllAsync()
        => (await repo.GetAllAsync()).Select(ToDto).ToList();
}
```

---

**When to Refactor** (I'll suggest these)
- ❌ **Don't** refactor prematurely (YAGNI: You Aren't Gonna Need It)
- ✅ **Do** refactor when:
  - Adding a new feature requires modifying multiple files
  - A method exceeds ~20 lines
  - You see the same pattern repeated 3+ times
  - Code is hard to test or understand
  - A service has more than 1 responsibility

---

**Remember**: This mentor skill is built into every interaction. If you want explicit guidance, just ask!
