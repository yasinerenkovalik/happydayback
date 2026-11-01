namespace HappyDay.Domain.Entities;

/// <summary>
/// Bir tarayıcıdaki giriş oturumu. Refresh token'ın yalnızca özeti tutulur; access token'daki
/// "sid" claim'i her istekte bu kayda bakar. Kayıt iptal edilince (çıkış, şifre değişikliği,
/// hesabın pasifleştirilmesi) o oturumun access token'ı da süresi dolmadan geçersiz olur.
/// </summary>
public class AuthSession
{
    public Guid Id { get; set; }

    /// <summary>Kullanıcı ya da firma Id'si</summary>
    public Guid AccountId { get; set; }

    /// <summary>"User" ya da "Company"</summary>
    public string AccountType { get; set; } = default!;

    /// <summary>Oturum açılırken verilen rol (Admin, Company, User); yenilemede yükseltilmez</summary>
    public string Role { get; set; } = default!;

    public string RefreshTokenHash { get; set; } = default!;

    /// <summary>Bir önceki refresh token; aynı anda gelen iki yenileme isteği kısa süre tolere edilir</summary>
    public string? PreviousRefreshTokenHash { get; set; }
    public DateTime? RotatedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Her yenilemede ileri kayar; bu süre boyunca kullanılmayan oturum biter</summary>
    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }
    public string? RevokeReason { get; set; }

    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
}
