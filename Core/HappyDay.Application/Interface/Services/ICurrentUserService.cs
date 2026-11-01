namespace HappyDay.Application.Interface.Services;

/// <summary>
/// İsteği yapan kullanıcının JWT'den okunan bilgileri.
/// Firma verisi üzerinde işlem yapan handler'lar sahiplik kontrolünü buradan yapar.
/// </summary>
public interface ICurrentUserService
{
    bool IsAdmin { get; }

    /// <summary>Token'daki rol: Admin, Company ya da User</summary>
    string? Role { get; }

    /// <summary>JWT'deki kullanıcı/firma Id'si (nameid).</summary>
    Guid? UserId { get; }
    Guid? CompanyId { get; }

    /// <summary>Access token'ın bağlı olduğu oturum (AuthSession)</summary>
    Guid? SessionId { get; }

    /// <summary>"Company" ya da "User": şifresi değişecek hesabın hangi tabloda olduğu</summary>
    string? AccountType { get; }

    /// <summary>Admin her firmayı, firma yetkilisi yalnızca kendi firmasını yönetebilir.</summary>
    bool CanManageCompany(Guid? companyId);
}
