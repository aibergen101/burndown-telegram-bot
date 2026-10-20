FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY YouTrackData/YouTrackData.csproj YouTrackData/
COPY YouTrackClient/YouTrackClient.csproj YouTrackClient/
COPY TelegramClient/TelegramClient.csproj TelegramClient/
RUN dotnet restore YouTrackData/YouTrackData.csproj

COPY YouTrackData/ YouTrackData/
COPY YouTrackClient/ YouTrackClient/
COPY TelegramClient/ TelegramClient/
RUN dotnet publish YouTrackData/YouTrackData.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
RUN apt-get update && apt-get install -y \
    fontconfig \
    fonts-dejavu-core \
    && fc-cache -f -v \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "YouTrackData.dll"]