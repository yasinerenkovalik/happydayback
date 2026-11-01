using HappyDay.Domain.Entities;

namespace HappyDay.Application.Interface.Repository;

public interface IAuthSessionRepository
{
    Task AddAsync(AuthSession session);
    Task<AuthSession?> GetAsync(Guid id);
    Task UpdateAsync(AuthSession session);

    /// <summary>Her yetkili istekte çağrılır: oturum var, iptal edilmemiş ve süresi dolmamış mı</summary>
    Task<bool> IsActiveAsync(Guid id);

    /// <summary>Hesabın açık oturumlarını kapatır; <paramref name="exceptSessionId"/> verilirse o oturum açık kalır</summary>
    Task<int> RevokeAllAsync(Guid accountId, string reason, Guid? exceptSessionId = null);
}
