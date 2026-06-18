# HoloNet.Video — Build Guide

Work through these steps in order. Each one builds on the previous.

---

## Step 1 — Clean up boilerplate in `Program.cs`
- [ ] Delete the `WeatherForecast` record at the bottom of `Program.cs`
- [ ] Delete the `/weatherforecast` endpoint
- Leave the `builder`/`app` setup intact — this is your clean starting point.

---

## Step 2 — Add `VideosPath` to `appsettings.json`
- [ ] Add the following to `appsettings.json`:
```json
"VideoService": {
  "VideosPath": "/data/videos"
}
```
> Locally, point this to any folder with test videos, e.g. `/Users/bjorn.goa/videos`.

---

## Step 3 — Create `VideoServiceOptions` config class
- [ ] Create a folder: `Configuration/`
- [ ] Create `Configuration/VideoServiceOptions.cs`:
```csharp
public class VideoServiceOptions
{
    public string VideosPath { get; set; } = string.Empty;
}
```
> This is the typed config object that maps to the `VideoService` section in `appsettings.json`.

---

## Step 4 — Bind `VideoServiceOptions` in `Program.cs`
- [ ] In `Program.cs`, after `var builder = ...`, add:
```csharp
builder.Services.Configure<VideoServiceOptions>(
    builder.Configuration.GetSection("VideoService"));
```
> Now `VideoServiceOptions` can be injected anywhere via `IOptions<VideoServiceOptions>`.

---

## Step 5 — Create `VideoDto` in `HoloNet.Shared`
- [ ] In `HoloNet.Shared`, create a folder: `Models/`
- [ ] Create `Models/VideoDto.cs`:
```csharp
public record VideoDto(
    string Id,
    string Title,
    string Extension,
    long FileSizeBytes,
    string StreamUrl
);
```
> This is the shape of data the API returns to clients.

---

## Step 6 — Create `IVideoService` interface
- [ ] Create a folder: `Services/`
- [ ] Create `Services/IVideoService.cs`:
```csharp
public interface IVideoService
{
    Task<IEnumerable<VideoDto>> GetAllAsync();
    Task<FileStream?> GetStreamAsync(string id);
}
```

---

## Step 7 — Implement `VideoService`
- [ ] Create `Services/VideoService.cs`
- [ ] Inject `IOptions<VideoServiceOptions>`
- [ ] Implement `GetAllAsync()`:
  - Scan `VideosPath` with `Directory.GetFiles()`
  - Filter to known extensions: `.mp4`, `.mkv`, `.avi`, `.mov`
  - For each file, generate a stable `Id` (e.g. `Convert.ToBase64String(Encoding.UTF8.GetBytes(filename))`)
  - Return a `VideoDto` per file
- [ ] Implement `GetStreamAsync(id)`:
  - Decode the `Id` back to a filename: `Encoding.UTF8.GetString(Convert.FromBase64String(id))`
  - Build the full path, check it exists
  - Return `new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)`

---

## Step 8 — Register `VideoService` in `Program.cs`
- [ ] Add to `Program.cs`:
```csharp
builder.Services.AddScoped<IVideoService, VideoService>();
```
> This wires the interface to the implementation through dependency injection.

---

## Step 9 — Build `GET /api/videos` endpoint
- [ ] Add to `Program.cs`:
```csharp
app.MapGet("/api/videos", async (IVideoService svc) =>
    await svc.GetAllAsync());
```
- [ ] Test: run `dotnet run`, open `http://localhost:5000/api/videos` in a browser or Postman
- [ ] Verify: you get a JSON array of video files from your test folder

---

## Step 10 — Build `GET /api/videos/{id}/stream` endpoint ⚡
This is the most important endpoint. It must support **HTTP range requests** so video players can seek (scrub) through the video without downloading the whole file.

- [ ] Add to `Program.cs`:
```csharp
app.MapGet("/api/videos/{id}/stream", async (string id, HttpContext ctx, IVideoService svc) =>
{
    var stream = await svc.GetStreamAsync(id);
    if (stream is null) return Results.NotFound();

    var contentType = Path.GetExtension(stream.Name).ToLower() switch
    {
        ".mp4"  => "video/mp4",
        ".mkv"  => "video/x-matroska",
        ".avi"  => "video/x-msvideo",
        ".mov"  => "video/quicktime",
        _       => "application/octet-stream"
    };

    return Results.Stream(stream, contentType, enableRangeProcessing: true);
});
```
> `enableRangeProcessing: true` is what makes seeking work — ASP.NET Core handles the range headers for you.

---

## Step 11 — Test video streaming in a browser
- [ ] Create a `test.html` file somewhere (outside the project):
```html
<video src="http://localhost:5000/api/videos/{PASTE_AN_ID_HERE}/stream" controls width="800"></video>
```
- [ ] Open in a browser
- [ ] Verify:
  - Video plays ✅
  - Scrubbing the timeline jumps to that position (doesn't restart) ✅ — this confirms range requests work

---

## Step 12 — Add `GET /health` endpoint
- [ ] Add to `Program.cs`:
```csharp
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "HoloNet.Video" }));
```
> Used by the Portal service to check if Video is running.

---

## Step 13 — Add CORS policy
- [ ] Add before `var app = builder.Build()`:
```csharp
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
```
- [ ] Add after `var app = builder.Build()`:
```csharp
app.UseCors();
```
> Without this, a browser on `portal.goa.no` will be blocked from calling `videos.goa.no/api/...`.

---

## Step 14 — Write the `Dockerfile`
- [ ] Create `Dockerfile` in the `HoloNet.Video/` folder:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ../HoloNet.Shared/HoloNet.Shared.csproj ../HoloNet.Shared/
COPY HoloNet.Video.csproj .
RUN dotnet restore
COPY . .
COPY ../HoloNet.Shared/ ../HoloNet.Shared/
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "HoloNet.Video.dll"]
```

---

## Step 15 — Add to `docker-compose.yml`
- [ ] Add to `docker-compose.yml` in the root `HoloNet/` folder:
```yaml
holonet-video:
  build:
    context: .
    dockerfile: HoloNet.Video/Dockerfile
  ports:
    - "5001:80"
  volumes:
    - ./data/videos:/data/videos
  environment:
    - VideoService__VideosPath=/data/videos
  networks:
    - holonet
```
> `VideoService__VideosPath` uses double underscores — this is how ASP.NET Core reads nested config from environment variables.
