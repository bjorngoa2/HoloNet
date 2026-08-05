FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY HoloNet.Shared/HoloNet.Shared.csproj HoloNet.Shared/
COPY HoloNet.Photos/HoloNet.Photos.csproj HoloNet.Photos/
RUN dotnet restore HoloNet.Photos/HoloNet.Photos.csproj
COPY HoloNet.Shared/ HoloNet.Shared/
COPY HoloNet.Photos/ HoloNet.Photos/
RUN dotnet publish HoloNet.Photos/HoloNet.Photos.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "HoloNet.Photos.dll"]
