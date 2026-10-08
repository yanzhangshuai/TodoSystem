FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TodoSystem.HealthDashboard/TodoSystem.HealthDashboard.csproj", "TodoSystem.HealthDashboard/"]
RUN dotnet restore "TodoSystem.HealthDashboard/TodoSystem.HealthDashboard.csproj"

COPY . .
WORKDIR "/src/TodoSystem.HealthDashboard"
RUN dotnet build "./TodoSystem.HealthDashboard.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TodoSystem.HealthDashboard.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TodoSystem.HealthDashboard.dll"]
