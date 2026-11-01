namespace HappyDay.Application.Features.Queries.Citys.GetAllCity;

public class GetAllCityQueryResponse
{
    public int Id { get; set; } 
    public string CityName { get; set; }

    /// <summary>İl merkezi; harita seçicisi il seçilince buraya odaklanır.</summary>
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
