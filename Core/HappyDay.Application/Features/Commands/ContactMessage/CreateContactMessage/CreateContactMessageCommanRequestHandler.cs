using System.Net;
using MapsterMapper;
using HappyDay.Application.Common.Email;
using HappyDay.Application.Interface.Repository;
using HappyDay.Application.Wrappers;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HappyDay.Application.Features.Commands.ContactMessage.CreateContactMessage
{
    public class CreateContactMessageCommanRequestHandler
        : IRequestHandler<CreateContactMessageCommanRequest, GeneralResponse<CreateContactMessageCommanResponse>>
    {
        private readonly IContactMessageRepository _contactMessageRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IMapper _mapper;
        private readonly IEmailSender _email;
        private readonly IConfiguration _config;
        private readonly ILogger<CreateContactMessageCommanRequestHandler> _logger;

        public CreateContactMessageCommanRequestHandler(
            IContactMessageRepository contactMessageRepository,
            IMapper mapper,
            IOrganizationRepository organizationRepository,
            ICompanyRepository companyRepository,
            IEmailSender email,
            IConfiguration config,
            ILogger<CreateContactMessageCommanRequestHandler> logger)
        {
            _contactMessageRepository = contactMessageRepository ?? throw new ArgumentNullException(nameof(contactMessageRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _organizationRepository = organizationRepository ?? throw new ArgumentNullException(nameof(organizationRepository));
            _companyRepository = companyRepository ?? throw new ArgumentNullException(nameof(companyRepository));
            _email = email ?? throw new ArgumentNullException(nameof(email));
            _config = config;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<GeneralResponse<CreateContactMessageCommanResponse>> Handle(
            CreateContactMessageCommanRequest request, CancellationToken cancellationToken)
        {
            if (request == null || request.OrganizationId == Guid.Empty)
            {
                return new GeneralResponse<CreateContactMessageCommanResponse>
                {
                    isSuccess = false,
                    Message = "Geçersiz istek.",
                    Errors = new[] { "Request null veya OrganizationId boş." }
                };
            }

            // Bot: başarılı görünsün ki form yeniden denenmesin
            if (!string.IsNullOrEmpty(request.Website))
            {
                _logger.LogInformation("Bal küpü alanı dolu iletişim mesajı yok sayıldı. OrganizationId: {OrganizationId}", request.OrganizationId);
                return new GeneralResponse<CreateContactMessageCommanResponse>
                {
                    isSuccess = true,
                    Message = "Mesajınız kaydedildi."
                };
            }

            // Organization kontrolü
            // Onaysız firmanın ya da yönetici kaldırmış olduğu mekana teklif gönderilemez
            var org = await _organizationRepository.GetPublishedByIdAsync(request.OrganizationId);
            if (org == null)
            {
                return new GeneralResponse<CreateContactMessageCommanResponse>
                {
                    isSuccess = false,
                    Message = "Organization bulunamadı.",
                    Errors = new[] { $"Organization not found: {request.OrganizationId}" }
                };
            }

            // Organization'a bağlı şirket bilgisi
            var companyId = org.CompanyId;
            if (companyId == Guid.Empty)
            {
                return new GeneralResponse<CreateContactMessageCommanResponse>
                {
                    isSuccess = false,
                    Message = "Organization'a bağlı şirket bulunamadı.",
                    Errors = new[] { "CompanyId boş." }
                };
            }

            // ContactMessage kaydı
            var contactMessage = _mapper.Map<Domain.Entities.ContactMessage>(request);
            contactMessage.CompanyId = companyId;

            // OrganizationId varsa set et
            var orgIdProp = contactMessage.GetType().GetProperty("OrganizationId");
            if (orgIdProp != null)
                orgIdProp.SetValue(contactMessage, request.OrganizationId);

            await _contactMessageRepository.AddAsync(contactMessage);

            // Şirket bilgisi ve e-posta gönderimi.
            // Mail gönderimi başarısız olursa mesaj kaydı kaybolmamalı; hata yakalanıp loglanır.
            var emailSent = false;
            try
            {
                var companyInfo = await _companyRepository.GetByIdAsync(companyId);
                if (companyInfo != null && !string.IsNullOrEmpty(companyInfo.Email))
                {
                    // Ziyaretçiden gelen her alan HTML'e kodlanır; aksi halde formdan e-postaya HTML/link enjekte edilebilir
                    static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty).Replace("\n", "<br>");
                    var panelLink = $"{_config["Frontend:BaseUrl"] ?? "https://mutlugunum.com"}/panel/mesajlar";

                    await _email.SendAsync(
                        companyInfo.Email,
                        $"Yeni teklif talebi: {org.Title}",
                        $@"<p><b>{Html(request.FullName)}</b>, <b>{Html(org.Title)}</b> için teklif istedi.</p>
                           <p><b>Telefon:</b> {Html(request.Phone)}<br><b>E-posta:</b> {Html(request.Email)}</p>
                           <p><b>Mesaj:</b><br>{Html(request.Message)}</p>
                           <p><a href=""{panelLink}"">Tüm talepleri panelde görün</a></p>",
                        $"{request.FullName}, {org.Title} için teklif istedi.\nTelefon: {request.Phone}\nE-posta: {request.Email}\n\n{request.Message}\n\nTüm talepler: {panelLink}",
                        cancellationToken);
                    emailSent = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İletişim mesajı e-posta ile gönderilemedi. CompanyId: {CompanyId}", companyId);
            }

            return new GeneralResponse<CreateContactMessageCommanResponse>
            {
                isSuccess = true,
                Message = emailSent
                    ? "Mesajınız kaydedildi ve işletmeye iletildi."
                    : "Mesajınız kaydedildi."
            };
        }
    }
}
