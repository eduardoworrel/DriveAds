# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY . .
RUN dotnet publish -c Release -o /out websocket.csproj

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app
COPY --from=build /out ./

RUN apt-get update && apt-get install -y ffmpeg

ENV ENV_PROJECT_NAME "./websocket"

EXPOSE 5017
ENV Urls="http://*:5017"
CMD $ENV_PROJECT_NAME