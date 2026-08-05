# Stage 1: Build React frontend
FROM oven/bun:1 AS frontend
WORKDIR /src/portal/ClientApp
COPY HoloNet.Portal/ClientApp/package.json HoloNet.Portal/ClientApp/bun.lock ./
RUN bun install --frozen-lockfile
COPY HoloNet.Portal/ClientApp/ .
RUN bun run build
# Output lands at /src/portal/wwwroot (outDir: '../wwwroot' in vite.config.ts)

# Stage 2: Build .NET app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY HoloNet.Shared/HoloNet.Shared.csproj HoloNet.Shared/
COPY HoloNet.Portal/HoloNet.Portal.csproj HoloNet.Portal/
RUN dotnet restore HoloNet.Portal/HoloNet.Portal.csproj
COPY HoloNet.Shared/ HoloNet.Shared/
COPY HoloNet.Portal/ HoloNet.Portal/
COPY --from=frontend /src/portal/wwwroot/ HoloNet.Portal/wwwroot/
RUN dotnet publish HoloNet.Portal/HoloNet.Portal.csproj -c Release -o /app/publish

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "HoloNet.Portal.dll"]
