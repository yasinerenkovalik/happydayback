# 1️⃣ Base imaj (runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# 2️⃣ Build imaj (SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Proje dosyalarını kopyala (paket sürümleri Directory.Packages.props'ta)
COPY ["Directory.Packages.props", "./"]
COPY ["Presentation/HappyDay.Api/HappyDay.Api.csproj", "Presentation/HappyDay.Api/"]
COPY ["Core/HappyDay.Application/HappyDay.Application.csproj", "Core/HappyDay.Application/"]
COPY ["Core/HappyDay.Domain/HappyDay.Domain.csproj", "Core/HappyDay.Domain/"]
COPY ["Infrastructure/HappyDay.Persistance/HappyDay.Persistance.csproj", "Infrastructure/HappyDay.Persistance/"]

# Bağımlılıkları indir
RUN dotnet restore "Presentation/HappyDay.Api/HappyDay.Api.csproj"

# Geri kalan dosyaları kopyala
COPY . .
WORKDIR "/src/Presentation/HappyDay.Api"

# Projeyi derle
RUN dotnet build "HappyDay.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 3️⃣ Publish aşaması
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "HappyDay.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 4️⃣ Final imaj (çalıştırma)
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

# Coolify ile uyumlu portta çalışması için:
ENV ASPNETCORE_URLS=http://+:8080

# Reverse proxy arkasında gerçek istemci IP'sini X-Forwarded-For'dan al.
# Aksi halde rate limiting tüm kullanıcıları proxy'nin tek IP'si olarak sayar.
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

ENTRYPOINT ["dotnet", "HappyDay.Api.dll"]
