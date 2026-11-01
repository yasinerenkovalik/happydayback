using MediatR;
using Microsoft.Extensions.Logging;
using HappyDay.Application.Wrappers;
using HappyDay.Application.Interface.Repository;

namespace HappyDay.Application.Features.PasswordReset.RequestPasswordReset;

public class RequestPasswordResetCommandHandler
    : IRequestHandler<RequestPasswordResetCommand, GeneralResponse<Unit>>
{
    private readonly ICompanyRepository _companies;
    private readonly PasswordReset.IPasswordResetService _service;
    private readonly ILogger<RequestPasswordResetCommandHandler> _logger;

    public RequestPasswordResetCommandHandler(
        ICompanyRepository companies,
        PasswordReset.IPasswordResetService service,
        ILogger<RequestPasswordResetCommandHandler> logger)
    {
        _logger = logger;
        _companies = companies;
        _service = service;
    }

    public async Task<GeneralResponse<Unit>> Handle(RequestPasswordResetCommand req, CancellationToken ct)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var company = await _companies.GetByEmailAsync(email);

        // Güvenlik: kullanıcıya "bu mail var mı yok mu" demiyoruz
        if (company is not null)
        {
            try
            {
                await _service.GenerateAndSendAsync(company.Id, company.Email, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şifre sıfırlama e-postası gönderilemedi. CompanyId: {CompanyId}", company.Id);
            }
        }

        return new GeneralResponse<Unit>
        {
            isSuccess = true,
            Message = "Eğer e-posta kayıtlıysa şifre sıfırlama bağlantısı gönderildi."
        };
    }
}