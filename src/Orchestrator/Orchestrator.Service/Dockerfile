FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Orchestrator/Orchestrator.Service/Orchestrator.Service.csproj", "Orchestrator/Orchestrator.Service/"]
RUN dotnet restore "Orchestrator/Orchestrator.Service/Orchestrator.Service.csproj"
COPY . .
WORKDIR "/src/Orchestrator/Orchestrator.Service"
RUN dotnet build "./Orchestrator.Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Orchestrator.Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Orchestrator.Service.dll"]
