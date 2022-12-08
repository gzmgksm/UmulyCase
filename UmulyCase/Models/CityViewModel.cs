namespace UmulyCase.Models
{
    public class CityViewModel
    {
        public int? CityId { get; set; }
        public string? CityName { get; set; }
        public int? CountryId { get; set; }
        public CountryViewModel? Country { get; set; }   
       
    }
}
