using HappyDay.Application.Common.Security;
using HappyDay.Application.Features.Commands.User.CreateUser;
using HappyDay.Application.Validations.UserValidator;
using HappyDay.Domain.Entities.BaseEntites;

namespace HappyDay.Tests.Unit;

public class ValidationRuleTests
{
    [Theory]
    [InlineData("10000000146", true)]
    [InlineData("12345678950", true)]
    [InlineData("12345678901", false)] // kontrol haneleri yanlış
    [InlineData("02345678950", false)] // 0 ile başlayamaz
    [InlineData("1000000014", false)]  // 10 hane
    [InlineData("1000000014a", false)]
    public void Turkish_identity_number_checksum(string value, bool valid) =>
        Assert.Equal(valid, UserCreateValidator.IsValidTurkishIdentityNo(value));

    [Theory]
    [InlineData("Sifre1234", true)]
    [InlineData("Şifreğüç9", true)]   // Türkçe harfler harf sayılır
    [InlineData("kisa1", false)]
    [InlineData("sadeceharf", false)]
    [InlineData("12345678", false)]
    public void Password_rule(string password, bool valid)
    {
        var result = new UserCreateValidator().Validate(new CreateUserCommandRequest { Email = "a@b.c", Password = password });
        Assert.Equal(valid, !result.Errors.Any(e => e.PropertyName == nameof(CreateUserCommandRequest.Password)));
    }

    private sealed class Account : ILockableAccount
    {
        public int FailedLoginCount { get; set; }
        public DateTime? LockoutEnd { get; set; }
    }

    [Fact]
    public void Lockout_starts_on_the_fifth_failure_and_reset_clears_it()
    {
        var account = new Account();
        for (var i = 0; i < LoginLockout.MaxFailedAttempts - 1; i++) LoginLockout.RegisterFailure(account);
        Assert.False(LoginLockout.IsLockedOut(account));

        LoginLockout.RegisterFailure(account);
        Assert.True(LoginLockout.IsLockedOut(account));

        Assert.True(LoginLockout.Reset(account));
        Assert.False(LoginLockout.IsLockedOut(account));
        Assert.False(LoginLockout.Reset(account)); // kaydedilecek değişiklik yok
    }

    [Fact]
    public void Expired_lockout_starts_counting_from_zero()
    {
        var account = new Account { FailedLoginCount = 0, LockoutEnd = DateTime.UtcNow.AddSeconds(-1) };

        LoginLockout.RegisterFailure(account);

        Assert.Null(account.LockoutEnd);
        Assert.Equal(1, account.FailedLoginCount);
    }
}
