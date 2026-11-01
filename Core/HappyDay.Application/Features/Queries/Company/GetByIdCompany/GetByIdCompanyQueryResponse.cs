namespace HappyDay.Application.Features.Queries.Company.GetByIdCompany;

public class GetByIdCompanyQueryResponse
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string Adress { get; set; }
    public string PhoneNumber { get; set; }
    public string Description { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? CoverPhotoPath { get; set; }
    public int  CityId { get; set; }
    public int  DistrictId { get; set; }

    /// <summary>Firma admin tarafından onaylanmadıkça mekanları sitede listelenmez.</summary>
    public bool IsApproved { get; set; }
    public bool IsEmailConfirmed { get; set; }
}