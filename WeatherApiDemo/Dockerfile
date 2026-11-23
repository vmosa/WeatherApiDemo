FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PlaygroundApp/PlaygroundApp.csproj", "PlaygroundApp/"]
COPY ["PlaygroundApp.ServiceDefaults/PlaygroundApp.ServiceDefaults.csproj", "PlaygroundApp.ServiceDefaults/"]
RUN dotnet restore "./PlaygroundApp/PlaygroundApp.csproj"
COPY PlaygroundApp/ PlaygroundApp/
COPY PlaygroundApp.ServiceDefaults/ PlaygroundApp.ServiceDefaults/
WORKDIR "/src/PlaygroundApp"
RUN dotnet build "./PlaygroundApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PlaygroundApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PlaygroundApp.dll"]
