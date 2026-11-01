using System.Text.Json.Serialization;
using HappyDay.Domain.Entities.BaseEntites;

namespace HappyDay.Domain.Entities;

public class User:BaseEntity, ILockableAccount
{
    public string Name { get; set; }
    public string Email { get; set; }
    [JsonIgnore] public string PasswordHash { get; set; } = default!;
    public string SurName { get; set; }
    public DateTime BirtDay { get; set; }
    /// <summary>T.C. kimlik no; 11 hane int'e sığmadığı için metin olarak tutulur.</summary>
    public string? IdentityNo { get; set; }
    public string Adress { get; set; }
    public string PhoneNumber { get; set; }
    public bool Sex { get; set; }

    /// <summary>Kullanıcının bağlı olduğu firma. Firma yetkilileri mesaj havuzunu buradan görür.</summary>
    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }

    /// <summary>
    /// Platform rolü. "Admin" yönetim paneline erişebilir, "User" erişemez.
    /// Firma yöneticiliği CompanyId üzerinden belirlenir.
    /// </summary>
    public string Role { get; set; } = "User";

    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEnd { get; set; }
}

