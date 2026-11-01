using FluentValidation;

namespace HappyDay.Application.Validations;

public static class PasswordRules
{
    public const int MinLength = 8;

    /// <summary>Kayıt ve şifre sıfırlamada ortak kural: en az 8 karakter, en az bir harf ve bir rakam.</summary>
    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Şifre zorunludur.")
            .MinimumLength(MinLength).WithMessage($"Şifre en az {MinLength} karakter olmalı.")
            .Matches(@"\p{L}").WithMessage("Şifre en az bir harf içermeli.")
            .Matches("[0-9]").WithMessage("Şifre en az bir rakam içermeli.");
}
