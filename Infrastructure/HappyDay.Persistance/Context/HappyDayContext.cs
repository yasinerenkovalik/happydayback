using HappyDay.Domain.Entities;
using HappyDay.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace HappyDay.Persistance.Context;

public class HappyDayContext : DbContext
{
    // Bağlantı Program.cs'te ConnectionStrings:DefaultConnection'dan, migration araçlarında
    // HappyDayContextFactory'den gelir. Burada sabit bir bağlantı yazılırsa o ikisini ezer.
    public HappyDayContext(DbContextOptions<HappyDayContext> options) : base(options) { }

    public DbSet<Company> Companies { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<OrganizationImage> OrganizationImages { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<District> Districts { get; set; }
    public DbSet<ContactMessage> ContactMessages { get; set; }
    public DbSet<CompanyInvitation> CompanyInvitations { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<Package> Packages { get; set; }
    public DbSet<CalendarEvent> CalendarEvents { get; set; }
    
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;
    public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; } = null!;
    public DbSet<AuthSession> AuthSessions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Reservation>()
            .HasOne(r => r.Organization)
            .WithMany(o => o.Reservations)
            .HasForeignKey(r => r.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict); 
        modelBuilder.Entity<CompanyInvitation>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.TokenHash).IsUnique(); // Token tekil olsun
            b.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
            b.Property(x => x.Email).HasMaxLength(320);
            b.Property(x => x.CompanyNameHint).HasMaxLength(200);
        });

        // Organization -> City / District: il veya ilçe silinirse mekanlar silinmesin
        modelBuilder.Entity<Organization>()
            .HasOne(o => o.City)
            .WithMany(c => c.Organizations)
            .HasForeignKey(o => o.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Organization>()
            .HasOne(o => o.District)
            .WithMany(d => d.Organizations)
            .HasForeignKey(o => o.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);

        // Mevcut mekanlar boş listeyle başlar; firma düzenlerken seçer
        modelBuilder.Entity<Organization>()
            .Property(o => o.EventTypeIds)
            .HasDefaultValueSql("'{}'::integer[]");


        modelBuilder.Entity<AuthSession>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.AccountId); // hesabın tüm oturumlarını kapatmak için
            b.Property(x => x.AccountType).IsRequired().HasMaxLength(16);
            b.Property(x => x.Role).IsRequired().HasMaxLength(16);
            b.Property(x => x.RefreshTokenHash).IsRequired().HasMaxLength(64);
            b.Property(x => x.PreviousRefreshTokenHash).HasMaxLength(64);
            b.Property(x => x.RevokeReason).HasMaxLength(32);
            b.Property(x => x.UserAgent).HasMaxLength(256);
            b.Property(x => x.IpAddress).HasMaxLength(64);
        });

        // District -> City
        modelBuilder.Entity<District>()
            .HasOne(d => d.City)
            .WithMany(c => c.Districts)
            .HasForeignKey(d => d.CityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}