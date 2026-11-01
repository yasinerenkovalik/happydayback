using System.Net;
using System.Text.Json.Serialization;
using FluentValidation;
using HappyDay.Application.Common.Email;
using HappyDay.Application.Interface.Repository;
using HappyDay.Application.Messages;
using HappyDay.Application.Wrappers;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HappyDay.Application.Features.Commands.Admin.ModerateOrganization;

/// <summary>Uygunsuz mekanı yayından kaldırır; mekan silinmez, firma sebebi panelde ve e-postada görür.</summary>
public class RemoveOrganizationCommand : IRequest<GeneralResponse<Unit>>
{
    [JsonIgnore] public Guid Id { get; set; } // adresten
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Yayından kaldırılmış mekanı yeniden yayına alır.</summary>
public record RestoreOrganizationCommand(Guid Id) : IRequest<GeneralResponse<Unit>>;

public class RemoveOrganizationValidator : AbstractValidator<RemoveOrganizationCommand>
{
    public RemoveOrganizationValidator()
    {
        RuleFor(x => x.Reason)
            .Must(r => !string.IsNullOrWhiteSpace(r) && r.Trim().Length >= 10)
            .WithMessage("Firmanın ne düzelteceğini anlaması için en az 10 karakterlik bir sebep yazın.")
            .MaximumLength(500).WithMessage("Sebep en fazla 500 karakter olabilir.");
    }
}

public class ModerateOrganizationCommandHandler :
    IRequestHandler<RemoveOrganizationCommand, GeneralResponse<Unit>>,
    IRequestHandler<RestoreOrganizationCommand, GeneralResponse<Unit>>
{
    private readonly IOrganizationRepository _organizations;
    private readonly ICompanyRepository _companies;
    private readonly IEmailSender _email;
    private readonly IConfiguration _config;
    private readonly ILogger<ModerateOrganizationCommandHandler> _logger;

    public ModerateOrganizationCommandHandler(IOrganizationRepository organizations, ICompanyRepository companies,
        IEmailSender email, IConfiguration config, ILogger<ModerateOrganizationCommandHandler> logger)
    {
        _organizations = organizations;
        _companies = companies;
        _email = email;
        _config = config;
        _logger = logger;
    }

    public async Task<GeneralResponse<Unit>> Handle(RemoveOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await _organizations.GetByIdAsync(request.Id);
        if (organization is null) return Fail(MessageConstants.OrganizationNotFound);

        organization.RemovedByAdminAt = DateTime.UtcNow;
        organization.RemovalReason = request.Reason.Trim();
        await _organizations.UpdateAsync(organization);

        var reason = WebUtility.HtmlEncode(organization.RemovalReason);
        await NotifyCompanyAsync(organization,
            $"Mekanınız yayından kaldırıldı: {organization.Title}",
            $@"<p><b>{WebUtility.HtmlEncode(organization.Title)}</b> mekanınız yönetici tarafından yayından kaldırıldı ve şu an sitede görünmüyor.</p>
               <p><b>Sebep:</b><br>{reason.Replace("\n", "<br>")}</p>
               <p>Mekanınız silinmedi. Sorunu panelden düzelttikten sonra bu e-postayı yanıtlayarak bize haber verin; inceleyip yeniden yayına alalım.</p>",
            $"{organization.Title} mekanınız yönetici tarafından yayından kaldırıldı.\nSebep: {organization.RemovalReason}\n\nSorunu düzelttikten sonra bu e-postayı yanıtlayarak bize haber verin.",
            cancellationToken);

        return Ok("Mekan yayından kaldırıldı; firmaya e-postayla bildirildi.");
    }

    public async Task<GeneralResponse<Unit>> Handle(RestoreOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await _organizations.GetByIdAsync(request.Id);
        if (organization is null) return Fail(MessageConstants.OrganizationNotFound);
        if (organization.RemovedByAdminAt is null) return Fail("Mekan zaten yayından kaldırılmamış.");

        organization.RemovedByAdminAt = null;
        organization.RemovalReason = null;
        await _organizations.UpdateAsync(organization);

        await NotifyCompanyAsync(organization,
            $"Mekanınız yeniden yayında: {organization.Title}",
            $"<p><b>{WebUtility.HtmlEncode(organization.Title)}</b> mekanınız yeniden yayına alındı ve sitede görünüyor.</p>",
            $"{organization.Title} mekanınız yeniden yayına alındı.",
            cancellationToken);

        return Ok("Mekan yeniden yayına alındı.");
    }

    // E-posta gidemese de moderasyon kararı geçerlidir; firma panelde de görür
    private async Task NotifyCompanyAsync(Domain.Entities.Organization organization, string subject, string html, string text, CancellationToken ct)
    {
        try
        {
            var company = await _companies.GetByIdAsync(organization.CompanyId);
            if (company is null) return;

            var panelLink = $"{_config["Frontend:BaseUrl"] ?? "http://mutlugunum.com.tr"}/panel/mekanlar/{organization.Id}";
            await _email.SendAsync(company.Email, subject,
                html + $@"<p><a href=""{panelLink}"">Mekanı panelde açın</a></p>",
                text + $"\n\nMekan: {panelLink}", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Moderasyon e-postası gönderilemedi. OrganizationId: {OrganizationId}", organization.Id);
        }
    }

    private static GeneralResponse<Unit> Ok(string message) => new() { isSuccess = true, Message = message };
    private static GeneralResponse<Unit> Fail(string message) => new() { isSuccess = false, Message = message };
}
