# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY . .
RUN dotnet publish -c Release -o /out websocket.csproj

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app
COPY --from=build /out ./

ENV ENV_PROJECT_NAME "./websocket"

EXPOSE 5017
ENV ASPNETCORE_URLS="http://+:5017"
CMD $ENV_PROJECT_NAME