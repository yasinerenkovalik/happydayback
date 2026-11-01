FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "./"]
COPY ["Presentation/HappyDay.Api/HappyDay.Api.csproj", "Presentation/HappyDay.Api/"]
COPY ["Core/HappyDay.Application/HappyDay.Application.csproj", "Core/HappyDay.Application/"]
COPY ["Core/HappyDay.Domain/HappyDay.Domain.csproj", "Core/HappyDay.Domain/"]
COPY ["Infrastructure/HappyDay.Persistance/HappyDay.Persistance.csproj", "Infrastructure/HappyDay.Persistance/"]
RUN dotnet restore "Presentation/HappyDay.Api/HappyDay.Api.csproj"
COPY . .
WORKDIR "/src/Presentation/HappyDay.Api"
RUN dotnet build "HappyDay.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "HappyDay.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Coolify'ın sağlık kontrolü (GET /health) container içinden curl ile yapılır.
# Yüklenen görseller kalıcı volume'a yazılır; uygulama root değil, $APP_UID kullanıcısıyla çalışır.
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/wwwroot/uploads && chown -R $APP_UID /app/wwwroot
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 CMD curl -fsS http://localhost:8080/health || exit 1
# Reverse proxy arkasında gerçek istemci IP'sini X-Forwarded-For'dan al (rate limiting için).
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ENTRYPOINT ["dotnet", "HappyDay.Api.dll"]
