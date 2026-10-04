FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY YouTrackData/YouTrackData.csproj .
RUN dotnet restore YouTrackData/YouTrackData.csproj
COPY . .
RUN dotnet publish YouTrackData/YouTrackData.csproj -c Release -o /app/publish
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "YouTrackData.dll"]
