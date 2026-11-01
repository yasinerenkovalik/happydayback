using FluentValidation;
using HappyDay.Application.Validations;
using HappyDay.Application.Features.Commands.Company.CreateCompany;
using HappyDay.Application.Features.Commands.Company.DeleteCompany;
using HappyDay.Application.Features.Commands.Company.UpdateCompany;

namespace HappyDay.Application.Validations.Company;

public class CompanyCreateValidator : AbstractValidator<CreateCompanyCommandRequest>
{
    public CompanyCreateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Firma adı zorunludur.").MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().WithMessage("E-posta zorunludur.")
            .EmailAddress().WithMessage(Messages.MessageConstants.InvalidEmail);
        RuleFor(x => x.Password).StrongPassword();
        RuleFor(x => x.Adress).NotEmpty().WithMessage("Adres zorunludur.");
        // Boşluk, parantez ve tire serbest; 10-13 rakam (0555..., 555..., +90555...)
        RuleFor(x => x.PhoneNumber)
            .Must(p => p is not null && p.Count(char.IsAsciiDigit) is >= 10 and <= 13)
            .WithMessage("Geçerli bir telefon numarası girin.");
        RuleFor(x => x.CityId).GreaterThan(0).WithMessage("İl seçin.");
        RuleFor(x => x.DistrictId).GreaterThan(0).WithMessage("İlçe seçin.");
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).WithMessage("Haritadan konum seçin.");
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).WithMessage("Haritadan konum seçin.");

    }
}

public class CompanyDeleteValidator : AbstractValidator<DeleteCompanyCommandRequest>
{
    public CompanyDeleteValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class CompanyUpdateValidator : AbstractValidator<UpdateCompanyCommandRequest>
{
    public CompanyUpdateValidator()
    {
        // Id adresten gelir (PUT api/companies/{id:guid}); otomatik validasyon controller
        // Id'yi atamadan önce çalıştığı için burada Id kuralı yok.
    }
}