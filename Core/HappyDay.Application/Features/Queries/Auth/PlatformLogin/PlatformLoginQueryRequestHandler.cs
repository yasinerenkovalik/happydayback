using HappyDay.Application.Common.Security;
using HappyDay.Application.Interface.Repository;
using HappyDay.Application.Wrappers;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace HappyDay.Application.Features.Queries.Auth.PlatformLogin;

public class PlatformLoginQueryRequestHandler
    : IRequestHandler<PlatformLoginQueryRequest, GeneralResponse<PlatformLoginQueryResponse>>
{
    private readonly IUserRepository _users;
    private readonly ICompanyRepository _companies;
    private readonly IAuthSessionService _sessions;
    private readonly BcryptPasswordHasher _pepperedHasher;
    private readonly BcryptPasswordHasher _noPepperHasher;

    private static readonly Regex BcryptPattern =
        new(@"^\$2[aby]\$\d{2}\$[./A-Za-z0-9]{53}$", RegexOptions.Compiled);

    public PlatformLoginQueryRequestHandler(
        IUserRepository users,
        ICompanyRepository companies,
        IAuthSessionService sessions,
        IConfiguration config)
    {
        _users = users;
        _companies = companies;
        _sessions = sessions;

        var security = config.GetSection("Security");
        var workFactor = security.GetValue<int?>("PasswordWorkFactor") ?? 12;
        var pepper = security.GetValue<string>("Pepper");

        _pepperedHasher = new BcryptPasswordHasher(workFactor, pepper);
        _noPepperHasher = new BcryptPasswordHasher(workFactor, null);
    }

    public async Task<GeneralResponse<PlatformLoginQueryResponse>> Handle(
        PlatformLoginQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Fail("E-posta ve şifre zorunludur.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var password = request.Password;

        // 1) Kullanıcı hesabı (admin / normal kullanıcı)
        // GetByEmailAsync pasif kayıtları filtreler — pasif hesap da "şifre hatalı" görünür.
        var user = await _users.GetByEmailAsync(email);

        if (user is not null)
        {
            if (LoginLockout.IsLockedOut(user))
            {
                return Fail(LoginLockout.LockedMessage(user));
            }

            if (!VerifyPassword(password, user.PasswordHash))
            {
                LoginLockout.RegisterFailure(user);
                await _users.UpdateAsync(user);

                return Fail(LoginLockout.IsLockedOut(user)
                    ? LoginLockout.LockedMessage(user)
                    : "E-posta veya şifre hatalı.");
            }

            if (LoginLockout.Reset(user))
            {
                await _users.UpdateAsync(user);
            }

            // Admin rolü yalnızca burada verilir
            return Success(await _sessions.StartAsync(AccountPrincipal.ForUser(user, allowAdmin: true)));
        }

        // 2) Firma hesabı
        var company = await _companies.GetByEmailAsync(email);

        if (company is not null)
        {
            if (!company.IsActivated)
            {
                return Fail("Bu firma hesabı pasif durumda.");
            }

            if (LoginLockout.IsLockedOut(company))
            {
                return Fail(LoginLockout.LockedMessage(company));
            }

            if (!VerifyPassword(password, company.PasswordHash))
            {
                LoginLockout.RegisterFailure(company);
                await _companies.UpdateAsync(company);

                return Fail(LoginLockout.IsLockedOut(company)
                    ? LoginLockout.LockedMessage(company)
                    : "E-posta veya şifre hatalı.");
            }

            if (LoginLockout.Reset(company))
            {
                await _companies.UpdateAsync(company);
            }

            if (!company.IsEmailConfirmed)
            {
                return new GeneralResponse<PlatformLoginQueryResponse>
                {
                    isSuccess = false,
                    Message = Messages.MessageConstants.EmailNotConfirmed,
                    Errors = new[] { Messages.MessageConstants.EmailNotConfirmedCode }
                };
            }

            return Success(await _sessions.StartAsync(AccountPrincipal.ForCompany(company)));
        }

        // Kullanıcı/şirket ayrımı yapılmaz — bilgi sızdırmamak için.
        return Fail("E-posta veya şifre hatalı.");
    }

    /// <summary>Hem pepper'lı hem pepper'sız (eski kayıtlar) hash'leri dener.</summary>
    private bool VerifyPassword(string input, string? storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash) || !BcryptPattern.IsMatch(storedHash))
        {
            return false;
        }

        if (_pepperedHasher.Verify(input, storedHash))
        {
            return true;
        }

        return _noPepperHasher.Verify(input, storedHash);
    }

    private static GeneralResponse<PlatformLoginQueryResponse> Success(AuthTokens tokens) =>
        new()
        {
            Data = new PlatformLoginQueryResponse { Token = tokens.AccessToken, Role = tokens.Principal.Role, Tokens = tokens },
            isSuccess = true,
            Message = "Giriş başarılı."
        };

    private static GeneralResponse<PlatformLoginQueryResponse> Fail(string message) =>
        new()
        {
            Data = null,
            isSuccess = false,
            Message = message
        };
}
