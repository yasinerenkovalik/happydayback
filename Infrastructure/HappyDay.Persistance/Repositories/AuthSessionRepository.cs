using HappyDay.Application.Interface.Repository;
using HappyDay.Domain.Entities;
using HappyDay.Persistance.Context;
using Microsoft.EntityFrameworkCore;

namespace HappyDay.Persistance.Repositories;

public class AuthSessionRepository : IAuthSessionRepository
{
    private readonly HappyDayContext _context;

    public AuthSessionRepository(HappyDayContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuthSession session)
    {
        _context.AuthSessions.Add(session);
        await _context.SaveChangesAsync();
    }

    public Task<AuthSession?> GetAsync(Guid id) =>
        _context.AuthSessions.FirstOrDefaultAsync(s => s.Id == id);

    public async Task UpdateAsync(AuthSession session)
    {
        _context.AuthSessions.Update(session);
        await _context.SaveChangesAsync();
    }

    public Task<bool> IsActiveAsync(Guid id)
    {
        var now = DateTime.UtcNow;
        return _context.AuthSessions.AsNoTracking()
            .AnyAsync(s => s.Id == id && s.RevokedAt == null && s.ExpiresAt > now);
    }

    public Task<int> RevokeAllAsync(Guid accountId, string reason, Guid? exceptSessionId = null)
    {
        var now = DateTime.UtcNow;
        return _context.AuthSessions
            .Where(s => s.AccountId == accountId && s.RevokedAt == null && s.Id != exceptSessionId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(s => s.RevokedAt, now)
                .SetProperty(s => s.RevokeReason, reason));
    }
}
