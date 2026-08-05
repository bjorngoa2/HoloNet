FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY HoloNet.Shared/HoloNet.Shared.csproj HoloNet.Shared/
COPY HoloNet.Games/HoloNet.Games.csproj HoloNet.Games/
RUN dotnet restore HoloNet.Games/HoloNet.Games.csproj
COPY HoloNet.Shared/ HoloNet.Shared/
COPY HoloNet.Games/ HoloNet.Games/
RUN dotnet publish HoloNet.Games/HoloNet.Games.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "HoloNet.Games.dll"]
