FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# 非 root 运行。emails 目录要在切用户之前建好并改属主，否则挂上卷后写不进去。
RUN mkdir -p /app/emails && chown $APP_UID /app/emails
USER $APP_UID

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TodoSystem.Web/TodoSystem.Web.csproj", "TodoSystem.Web/"]
COPY ["TodoSystem.Application/TodoSystem.Application.csproj", "TodoSystem.Application/"]
COPY ["TodoSystem.Infrastructure/TodoSystem.Infrastructure.csproj", "TodoSystem.Infrastructure/"]
COPY ["TodoSystem.EntityFrameworkCore/TodoSystem.EntityFrameworkCore.csproj", "TodoSystem.EntityFrameworkCore/"]
COPY ["TodoSystem.Domain/TodoSystem.Domain.csproj", "TodoSystem.Domain/"]
RUN dotnet restore "TodoSystem.Web/TodoSystem.Web.csproj"

COPY . .
WORKDIR "/src/TodoSystem.Web"
RUN dotnet build "./TodoSystem.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TodoSystem.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TodoSystem.Web.dll"]
