using MapsterMapper;
using FluentValidation;
using HappyDay.Application.Common.Security;
using HappyDay.Application.Features.EmailVerification; // IPasswordHasher
using HappyDay.Application.Interface.Repository;
using HappyDay.Application.Interface.Services;
using HappyDay.Application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HappyDay.Application.Features.Commands.Company.CreateCompany;

public class CreateCompanyCommandRequestHandler
    : IRequestHandler<CreateCompanyCommandRequest, GeneralResponse<CreateCompanyCommandResponse>>
{
    private readonly ICompanyRepository _repository;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateCompanyCommandRequest> _validator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly IFileService _fileService;
    private readonly ILogger<CreateCompanyCommandRequestHandler> _logger;

    public CreateCompanyCommandRequestHandler(
        ICompanyRepository repository,
        IMapper mapper,
        
        IValidator<CreateCompanyCommandRequest> validator,
        IPasswordHasher passwordHasher, IEmailVerificationService emailVerificationService, IFileService fileService,
        ILogger<CreateCompanyCommandRequestHandler> logger)
    {
        _logger = logger;
        _repository = repository;
        _mapper = mapper;
        _validator = validator;
        _passwordHasher = passwordHasher;
        _emailVerificationService = emailVerificationService;
        _fileService = fileService;
    }

    public async Task<GeneralResponse<CreateCompanyCommandResponse>> Handle(
        CreateCompanyCommandRequest request,
        CancellationToken cancellationToken)
    {
        // 1) Validasyon
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new GeneralResponse<CreateCompanyCommandResponse>
            {
                Message = validationResult.Errors.First().ErrorMessage,
                isSuccess = false
            };
        }

        // 2) E-posta normalize + tekillik kontrolü
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var exists = await _repository.GetByEmailAsync(email) is not null;
        if (exists)
        {
            return new GeneralResponse<CreateCompanyCommandResponse>
            {
                Message = "Bu e-posta ile kayıtlı bir şirket zaten var.",
                isSuccess = false
            };
        }

    
        var company = _mapper.Map<Domain.Entities.Company>(request);
        company.Email = email; // normalize edilmiş hali yaz
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            company.PasswordHash = _passwordHasher.Hash(request.Password);
        }
        // Kapak fotoğrafı kayıtta isteğe bağlı; panelden sonra eklenebilir
        if (request.CoverPhotoPath is not null)
        {
            company.CoverPhotoPath = await _fileService.SaveFileAsync(request.CoverPhotoPath, "uploads/companycover");
        }

        await _repository.AddAsync(company);

        // Firma kaydedildi; doğrulama e-postası gönderilemese de kayıt başarılıdır.
        // Firma panelden "tekrar gönder" ile yeniden isteyebilir.
        try
        {
            await _emailVerificationService.GenerateAndSendAsync(company.Id, company.Email, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Doğrulama e-postası gönderilemedi. CompanyId: {CompanyId}", company.Id);
            return new GeneralResponse<CreateCompanyCommandResponse>
            {
                Message = "Hesabınız oluşturuldu ancak doğrulama e-postası gönderilemedi. Panelden tekrar gönderebilirsiniz.",
                isSuccess = true
            };
        }

        return new GeneralResponse<CreateCompanyCommandResponse>
        {
            Message = Messages.MessageConstants.CompanyCreated,
            isSuccess = true
        };
    }
}
