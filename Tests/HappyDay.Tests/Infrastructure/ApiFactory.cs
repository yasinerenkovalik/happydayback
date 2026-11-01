using HappyDay.Application.Common.Email;
using HappyDay.Application.Interface.Services;
using HappyDay.Domain.Entities;
using HappyDay.Persistance.Context;
using Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HappyDay.Tests.Infrastructure;

/// <summary>
/// Gerçek Postgres'e karşı çalışan API. Her test çalıştırması kendine ait boş bir veritabanı açar,
/// migration'ları uygular ve bitince siler; geliştirme veritabanına dokunulmaz.
/// Postgres adresi HAPPYDAY_TEST_DB ortam değişkeninden gelir (varsayılan: docker-compose.dev.yml).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databaseName = $"happyday_test_{Guid.NewGuid():N}";
    private readonly string _server = Environment.GetEnvironmentVariable("HAPPYDAY_TEST_DB")
        ?? "Host=localhost;Port=5432;Username=postgres;Password=postgres";

    public string ConnectionString => new NpgsqlConnectionStringBuilder(_server) { Database = _databaseName }.ConnectionString;

    /// <summary>Yüklenen dosyalar projenin wwwroot'una değil buraya yazılır</summary>
    public string UploadRoot { get; } = Directory.CreateTempSubdirectory("happyday-uploads-").FullName;

    public FakeEmailSender Emails { get; } = new();
    public int CategoryId { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("JwtSettings:Secret", "test-secret-en-az-otuz-iki-karakter-uzunlugunda");
        builder.UseSetting("Security:Pepper", "test-pepper");
        builder.UseSetting("Security:PasswordWorkFactor", "4"); // bcrypt testlerde hızlı çalışsın
        builder.UseSetting("RateLimiting:Enabled", "false");    // aynı istemciden onlarca giriş yapılıyor
        builder.UseSetting("Frontend:BaseUrl", "https://frontend.test");

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IEmailSender>(Emails);
            services.AddScoped<IFileService>(sp => new FileService(
                new UploadRootEnvironment(sp.GetRequiredService<IWebHostEnvironment>(), UploadRoot),
                sp.GetRequiredService<ILogger<FileService>>()));
        });
    }

    public async Task InitializeAsync()
    {
        await ExecuteOnServerAsync($"CREATE DATABASE \"{_databaseName}\"");

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyDayContext>();
        await db.Database.MigrateAsync(); // il/ilçeler de migration'la gelir

        var category = new Category { Name = "Test Kategorisi" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        CategoryId = category.Id;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        await ExecuteOnServerAsync($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)");
        Directory.Delete(UploadRoot, recursive: true);
    }

    /// <summary>Çerezleri saklayan, CSRF başlığını gönderen istemci: tarayıcı gibi davranır</summary>
    public HttpClient Browser()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), // Secure cookie'ler gönderilsin
            HandleCookies = true,
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Add(Http.ClientHeader, "web");
        return client;
    }

    /// <summary>Çerez saklamayan istemci: token'lar ve çerezler elle verilir</summary>
    public HttpClient Client(bool clientHeader = true)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = false,
            AllowAutoRedirect = false,
        });
        if (clientHeader) client.DefaultRequestHeaders.Add(Http.ClientHeader, "web");
        return client;
    }

    public async Task<T> WithDbAsync<T>(Func<HappyDayContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<HappyDayContext>());
    }

    public T GetService<T>(Func<IServiceProvider, T> resolve)
    {
        using var scope = Services.CreateScope();
        return resolve(scope.ServiceProvider);
    }

    private async Task ExecuteOnServerAsync(string sql)
    {
        var builder = new NpgsqlConnectionStringBuilder(_server) { Database = "postgres" };
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>FileService'in gördüğü web kökünü geçici klasöre çevirir; geri kalan her şey uygulamanınki</summary>
    private sealed class UploadRootEnvironment(IWebHostEnvironment inner, string webRoot) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = webRoot;
        public IFileProvider WebRootFileProvider { get; set; } = new PhysicalFileProvider(webRoot);
        public string ApplicationName { get => inner.ApplicationName; set => inner.ApplicationName = value; }
        public IFileProvider ContentRootFileProvider { get => inner.ContentRootFileProvider; set => inner.ContentRootFileProvider = value; }
        public string ContentRootPath { get => inner.ContentRootPath; set => inner.ContentRootPath = value; }
        public string EnvironmentName { get => inner.EnvironmentName; set => inner.EnvironmentName = value; }
    }
}

/// <summary>Tüm API testleri aynı veritabanını paylaşır ve sırayla çalışır; her test kendi e-postalarını üretir</summary>
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
