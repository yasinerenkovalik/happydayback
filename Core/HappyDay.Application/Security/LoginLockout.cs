using HappyDay.Domain.Entities.BaseEntites;

namespace HappyDay.Application.Common.Security;

/// <summary>
/// Hesap bazlı giriş kilidi. IP bazlı rate limit dağıtık denemeyi durdurmaz;
/// aynı hesaba art arda <see cref="MaxFailedAttempts"/> hatalı şifre girilince hesap
/// <see cref="LockoutDuration"/> boyunca kilitlenir. Çağıran, değişen hesabı kaydetmelidir.
/// </summary>
public static class LoginLockout
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public static bool IsLockedOut(ILockableAccount account) =>
        account.LockoutEnd is { } end && end > DateTime.UtcNow;

    public static string LockedMessage(ILockableAccount account)
    {
        var minutes = account.LockoutEnd is { } end
            ? Math.Max(1, (int)Math.Ceiling((end - DateTime.UtcNow).TotalMinutes))
            : (int)LockoutDuration.TotalMinutes;

        return $"Çok fazla hatalı giriş denemesi yapıldı. {minutes} dakika sonra tekrar deneyin.";
    }

    /// <summary>Hatalı şifreyi sayar; sınıra ulaşılırsa hesabı kilitler.</summary>
    public static void RegisterFailure(ILockableAccount account)
    {
        // Süresi dolmuş kilitten sonra sayaç baştan başlar
        if (account.LockoutEnd is { } end && end <= DateTime.UtcNow)
        {
            account.LockoutEnd = null;
            account.FailedLoginCount = 0;
        }

        account.FailedLoginCount++;

        if (account.FailedLoginCount >= MaxFailedAttempts)
        {
            account.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
            account.FailedLoginCount = 0;
        }
    }

    /// <summary>Başarılı girişte sayacı sıfırlar. Kaydedilecek bir değişiklik varsa true döner.</summary>
    public static bool Reset(ILockableAccount account)
    {
        if (account.FailedLoginCount == 0 && account.LockoutEnd is null)
            return false;

        account.FailedLoginCount = 0;
        account.LockoutEnd = null;
        return true;
    }
}
