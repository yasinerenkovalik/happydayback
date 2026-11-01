using FluentValidation;
using HappyDay.Application.Features.PasswordReset.ResetPassword;

namespace HappyDay.Application.Validations.Company;

public class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage("Geçersiz bağlantı.");
        RuleFor(x => x.Token).NotEmpty().WithMessage("Geçersiz bağlantı.");
        RuleFor(x => x.NewPassword).StrongPassword();
    }
}
