FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.sln ./
COPY DiscordMultiRP.Bot/DiscordMultiRP.Bot.csproj ./DiscordMultiRP.Bot/
COPY DiscordMultiRP.Web/DiscordMultiRP.Web.csproj ./DiscordMultiRP.Web/
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Debug -o /app/out/bot DiscordMultiRP.Bot/DiscordMultiRP.Bot.csproj && \
    dotnet publish -c Debug -o /app/out/web DiscordMultiRP.Web/DiscordMultiRP.Web.csproj && \
    cp NLog.config out/bot/ && cp NLog.config out/web/

# Build runtime images
FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 AS web
WORKDIR /app
COPY --from=build-env /app/out/web .
ENTRYPOINT ["dotnet", "DiscordMultiRP.Web.dll"]

FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 AS bot
WORKDIR /app
COPY --from=build-env /app/out/bot .
ENTRYPOINT ["dotnet", "DiscordMultiRP.Bot.dll"]

