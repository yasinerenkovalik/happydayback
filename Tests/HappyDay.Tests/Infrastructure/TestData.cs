using HappyDay.Application.Common.Security;
using HappyDay.Domain.Entities;
using HappyDay.Persistance.Context;
using Microsoft.Extensions.DependencyInjection;

namespace HappyDay.Tests.Infrastructure;

/// <summary>Testlerin kendi kayıtları; her test benzersiz e-posta kullandığı için birbirini etkilemez</summary>
public static class TestData
{
    public const string Password = "Sifre1234";

    // İstanbul (plaka 34) ve ilk ilçesi; SeedTurkeyLocations migration'ından gelir
    public const int Istanbul = 34;
    public const int IstanbulDistrict = 34001;
    public const int Ankara = 6;

    public static string Email(string prefix) => $"{prefix}-{Guid.NewGuid():N}@test.local";

    public static async Task<Company> CreateCompanyAsync(this ApiFactory factory, bool approved = true, string? email = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyDayContext>();
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Test Firma",
            Email = email ?? Email("firma"),
            PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Hash(Password),
            Adress = "Test Mah. No:1",
            PhoneNumber = "05550000000",
            Description = "Test firması",
            Latitude = 41.0m,
            Longitude = 29.0m,
            CityId = Istanbul,
            DistrictId = IstanbulDistrict,
            IsApproved = approved,
            IsEmailConfirmed = true,
            IsActivated = true,
            CreateDate = DateTime.UtcNow,
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        return company;
    }

    public static Task<User> CreateAdminAsync(this ApiFactory factory) => factory.CreateUserAsync(role: "Admin");

    public static async Task<User> CreateUserAsync(this ApiFactory factory, string role = "User")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyDayContext>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            SurName = role,
            Email = Email(role.ToLowerInvariant()),
            PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Hash(Password),
            BirtDay = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Adress = "Adres",
            PhoneNumber = "05550000000",
            Role = role,
            IsActivated = true,
            CreateDate = DateTime.UtcNow,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public static async Task<Organization> CreateVenueAsync(this ApiFactory factory, Guid companyId, string? coverPhotoPath = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyDayContext>();
        var venue = new Organization
        {
            Id = Guid.NewGuid(),
            Title = "Test Mekanı",
            CategoryId = factory.CategoryId,
            Description = "Açıklama",
            Price = 50000,
            MaxGuestCount = 200,
            Services = ["DJ"],
            Duration = "6 saat",
            CompanyId = companyId,
            CoverPhotoPath = coverPhotoPath,
            CityId = Istanbul,
            DistrictId = IstanbulDistrict,
            Adress = "Mekan Sok. No:2",
            Latitude = 41.01m,
            Longitude = 29.01m,
            IsActivated = true,
            CreateDate = DateTime.UtcNow,
        };
        db.Organizations.Add(venue);
        await db.SaveChangesAsync();
        return venue;
    }

    /// <summary>Tarayıcı gibi giriş yapmış firma istemcisi</summary>
    public static async Task<HttpClient> LoggedInCompanyAsync(this ApiFactory factory, Company company)
    {
        var browser = factory.Browser();
        var response = await browser.LoginCompanyAsync(company.Email, Password);
        Assert.True((await response.ResultAsync()).Ok, "firma girişi başarısız");
        return browser;
    }

    public static async Task<HttpClient> LoggedInAdminAsync(this ApiFactory factory)
    {
        var admin = await factory.CreateAdminAsync();
        var browser = factory.Browser();
        var response = await browser.LoginPlatformAsync(admin.Email, Password);
        Assert.True((await response.ResultAsync()).Ok, "admin girişi başarısız");
        return browser;
    }

    /// <summary>Uzantının beklediği imzayla başlayan "görsel" baytları (FileService içeriği çözmez, imzaya bakar)</summary>
    public static class Images
    {
        public static byte[] Png => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, .. new byte[64]];
        public static byte[] Jpeg => [0xFF, 0xD8, 0xFF, 0xE0, .. new byte[64]];
        public static byte[] Gif => [.. "GIF89a"u8.ToArray(), .. new byte[32]];
    }
}
