using System.Text.RegularExpressions;
using HappyDay.Application.Common.Security;
using HappyDay.Application.Interface.Repository;
using HappyDay.Application.Interface.Services;
using HappyDay.Application.Wrappers;
using HappyDay.Domain.Entities.BaseEntites;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace HappyDay.Application.Features.Commands.Auth.ChangePassword;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, GeneralResponse<Unit>>
{
    private static readonly Regex BcryptPattern = new(@"^\$2[aby]\$\d{2}\$[./A-Za-z0-9]{53}$", RegexOptions.Compiled);

    private readonly ICurrentUserService _currentUser;
    private readonly ICompanyRepository _companies;
    private readonly IUserRepository _users;
    private readonly IAuthSessionRepository _sessions;
    private readonly BcryptPasswordHasher _pepperedHasher;
    private readonly BcryptPasswordHasher _noPepperHasher;

    public ChangePasswordCommandHandler(ICurrentUserService currentUser, ICompanyRepository companies, IUserRepository users,
        IAuthSessionRepository sessions, IConfiguration config)
    {
        _currentUser = currentUser;
        _companies = companies;
        _users = users;
        _sessions = sessions;

        var security = config.GetSection("Security");
        var workFactor = security.GetValue<int?>("PasswordWorkFactor") ?? 12;
        _pepperedHasher = new BcryptPasswordHasher(workFactor, security.GetValue<string>("Pepper"));
        _noPepperHasher = new BcryptPasswordHasher(workFactor, null);
    }

    public async Task<GeneralResponse<Unit>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } accountId)
            return Fail("Oturum bulunamadı, lütfen tekrar giriş yapın.");

        // Firma hesabı ile kullanıcı hesabı ayrı tablolarda; token'daki "acct" claim'i hangisi olduğunu söyler
        if (_currentUser.AccountType == AccountPrincipal.CompanyAccount)
        {
            var company = await _companies.GetByIdAsync(accountId);
            if (company is null) return Fail("Hesap bulunamadı.");
            return await ChangeAsync(company, company.PasswordHash, hash => company.PasswordHash = hash,
                () => _companies.UpdateAsync(company), request);
        }

        var user = await _users.GetByIdAsync(accountId);
        if (user is null) return Fail("Hesap bulunamadı.");
        return await ChangeAsync(user, user.PasswordHash, hash => user.PasswordHash = hash,
            () => _users.UpdateAsync(user), request);
    }

    private async Task<GeneralResponse<Unit>> ChangeAsync(ILockableAccount account, string storedHash, Action<string> setHash,
        Func<Task> save, ChangePasswordCommand request)
    {
        // Çalınmış bir oturumla mevcut şifre denenemesin: hatalı denemeler giriş kilidine sayılır
        if (LoginLockout.IsLockedOut(account))
            return Fail(LoginLockout.LockedMessage(account));

        if (!Verify(request.CurrentPassword, storedHash))
        {
            LoginLockout.RegisterFailure(account);
            await save();
            return Fail(LoginLockout.IsLockedOut(account) ? LoginLockout.LockedMessage(account) : "Mevcut şifre hatalı.");
        }

        setHash(_pepperedHasher.Hash(request.NewPassword));
        LoginLockout.Reset(account);
        await save();

        // Bu tarayıcı açık kalır, diğer cihazlardaki oturumlar kapanır
        await _sessions.RevokeAllAsync(_currentUser.UserId!.Value, "password_changed", _currentUser.SessionId);

        return new GeneralResponse<Unit>
        {
            isSuccess = true,
            Message = "Şifreniz değiştirildi. Diğer cihazlardaki oturumlarınız kapatıldı."
        };
    }

    private bool Verify(string input, string? storedHash) =>
        !string.IsNullOrWhiteSpace(storedHash) && BcryptPattern.IsMatch(storedHash) &&
        (_pepperedHasher.Verify(input, storedHash) || _noPepperHasher.Verify(input, storedHash));

    private static GeneralResponse<Unit> Fail(string message) => new() { isSuccess = false, Message = message };
}
