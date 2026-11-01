using HappyDay.Domain.Entities.BaseEntites;

namespace HappyDay.Domain.Entities;

public class Organization:BaseEntity
{
    public string Title { get; set; }
    public int CategoryId { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int MaxGuestCount { get; set; }

   
    public List<string> Services { get; set; } = new();
    public string Duration { get; set; }
    public bool IsOutdoor { get; set; }
    public string? ReservationNote { get; set; }
    public string? CancelPolicy { get; set; }
    public string? VideoUrl { get; set; }
    public bool IsFeatured { get; set; } 
    
    public Guid CompanyId { get; set; }
    public Company Company { get; set; }
    public string? CoverPhotoPath { get; set; }

    // Mekanın kendi konumu. Firma adresinden bağımsızdır: bir firmanın farklı ilçe ya da
    // şehirlerde mekanı olabilir. Gönderilmezse kayıt sırasında firmanın konumu kopyalanır.
    public int CityId { get; set; }
    public City City { get; set; }
    public int DistrictId { get; set; }
    public District District { get; set; }
    public string Adress { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }

    // Yönetici uygunsuz bulup yayından kaldırdıysa: mekan silinmez, sitede görünmez; firma sebebi panelde görür
    public DateTime? RemovedByAdminAt { get; set; }
    public string? RemovalReason { get; set; }
    public ICollection<Reservation> Reservations { get; set; }
    public ICollection<OrganizationImage> OrganizationImages { get; set; }
    public ICollection<Package> Packages { get; set; } = new List<Package>();


}