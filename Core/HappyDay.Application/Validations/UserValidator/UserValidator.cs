using FluentValidation;
using HappyDay.Application.Validations;
using HappyDay.Application.Features.Commands.User.CreateUser;

namespace HappyDay.Application.Validations.UserValidator;

public class UserCreateValidator:AbstractValidator<CreateUserCommandRequest>
{
    public UserCreateValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required");
        RuleFor(x => x.Password).StrongPassword();

        // İsteğe bağlı; gönderilirse geçerli bir T.C. kimlik no olmalı
        RuleFor(x => x.IdentityNo)
            .Must(IsValidTurkishIdentityNo!)
            .When(x => !string.IsNullOrWhiteSpace(x.IdentityNo))
            .WithMessage("Geçersiz T.C. kimlik numarası.");
    }

    /// <summary>11 hane, ilk hane 0 değil, 10. ve 11. haneler kontrol hanesi.</summary>
    public static bool IsValidTurkishIdentityNo(string value)
    {
        if (value.Length != 11 || value[0] == '0' || !value.All(char.IsAsciiDigit))
            return false;

        var d = value.Select(c => c - '0').ToArray();
        var odd = d[0] + d[2] + d[4] + d[6] + d[8];
        var even = d[1] + d[3] + d[5] + d[7];

        var tenth = ((odd * 7 - even) % 10 + 10) % 10;
        var eleventh = d.Take(10).Sum() % 10;

        return d[9] == tenth && d[10] == eleventh;
    }
}
