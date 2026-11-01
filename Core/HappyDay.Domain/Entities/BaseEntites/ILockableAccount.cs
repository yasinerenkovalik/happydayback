namespace HappyDay.Domain.Entities.BaseEntites;

/// <summary>Art arda hatalı girişte geçici olarak kilitlenebilen hesap (kullanıcı ve firma).</summary>
public interface ILockableAccount
{
    int FailedLoginCount { get; set; }

    /// <summary>UTC. Bu zamana kadar giriş denemeleri şifreye bakılmadan reddedilir.</summary>
    DateTime? LockoutEnd { get; set; }
}
