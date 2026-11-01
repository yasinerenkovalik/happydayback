namespace HappyDay.Application.Features.Commands.Organization.CreateOrganization;

public class CreateOrganizationCommandResponse
{
    /// <summary>Yeni mekanın Id'si; panel oluşturma sonrası düzenleme sayfasına buradan geçer.</summary>
    public Guid Id { get; set; }
}
