namespace UmulyCase.Models
{
    public class AdressViewModel
    {
        public CountryViewModel? Country { get; set; } = new CountryViewModel();
        public CityViewModel? City { get; set; } = new CityViewModel();
    }
}
