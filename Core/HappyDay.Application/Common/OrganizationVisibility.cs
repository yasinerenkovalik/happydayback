using System.Linq.Expressions;
using HappyDay.Domain.Entities;

namespace HappyDay.Application.Common;

public static class OrganizationVisibility
{
    /// <summary>
    /// Sitede görünen mekan: mekan silinmemiş, yönetici yayından kaldırmamış, firması aktif ve onaylı.
    /// Herkese açık bütün sorgular (liste, detay, harita, teklif formu) bu kuralı kullanır.
    /// </summary>
    public static readonly Expression<Func<Organization, bool>> Published = o =>
        o.IsActivated && o.RemovedByAdminAt == null && o.Company.IsActivated && o.Company.IsApproved;
}
