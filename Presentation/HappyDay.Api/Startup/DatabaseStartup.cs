using HappyDay.Application.Common.Security;
using HappyDay.Domain.Entities;
using HappyDay.Persistance.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HappyDay.Api.Startup;

public static class DatabaseStartup
{
    /// <summary>
    /// Sunucuda açılışta çalışır:
    /// - Database:MigrateOnStartup=true ise bekleyen migration'ları uygular (il/ilçe ve kategoriler dahil).
    /// - Bootstrap:AdminEmail ve Bootstrap:AdminPassword verilmişse ve hiç yönetici yoksa ilk yöneticiyi oluşturur.
    ///   Oluşturduktan sonra bu iki ayar silinmeli; yönetici varken tekrar çalışmaz.
    /// </summary>
    public static async Task PrepareDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyDayContext>();
        var config = app.Configuration;

        if (config.GetValue("Database:MigrateOnStartup", false))
        {
            app.Logger.LogInformation("Bekleyen migration'lar uygulanıyor…");
            await db.Database.MigrateAsync();
        }

        var email = config["Bootstrap:AdminEmail"]?.Trim().ToLowerInvariant();
        var password = config["Bootstrap:AdminPassword"];
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) return;

        if (await db.Users.AnyAsync(u => u.Role == "Admin" && u.IsActivated))
        {
            app.Logger.LogWarning("Bootstrap:AdminEmail/AdminPassword ayarlı ama zaten bir yönetici var; bu iki ayarı silin.");
            return;
        }
        if (password.Length < 8 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            app.Logger.LogError("İlk yönetici oluşturulmadı: Bootstrap:AdminPassword en az 8 karakter olmalı, harf ve rakam içermeli.");
            return;
        }
        if (await db.Users.AnyAsync(u => u.Email.ToLower() == email && u.IsActivated))
        {
            app.Logger.LogError("İlk yönetici oluşturulmadı: {Email} ile zaten bir kullanıcı var.", email);
            return;
        }

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Name = "Site",
            SurName = "Yöneticisi",
            Email = email,
            PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Hash(password),
            BirtDay = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Adress = "-",
            PhoneNumber = "-",
            Role = "Admin",
            IsActivated = true,
            CreateDate = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        app.Logger.LogWarning("İlk yönetici oluşturuldu: {Email}. Bootstrap__AdminEmail ve Bootstrap__AdminPassword ayarlarını şimdi silin.", email);
    }
}

/// <summary>GET /health: Coolify'ın sağlık kontrolü; veritabanına bağlanılamıyorsa 503 döner</summary>
public class DatabaseHealthCheck(HappyDayContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Veritabanına bağlanılamıyor.");
}
