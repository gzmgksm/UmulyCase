namespace UmulyCase.Models
{
    public class Rootobject
    {
        public JsonObjectData[]? Value { get; set; }
    }

    public class JsonObjectData
    {
        public int? Id { get; set; }
        public string? OfferDate { get; set; }
        public string? Description { get; set; }
        public string? UserName { get; set; }
        public List<Detail>? Details { get; set; }
    }

    public class Detail
    {
        public int Id { get; set; }
        public int OfferId { get; set; }
        public List<ModeViewModel> Mode { get; set; } = new List<ModeViewModel>();
        public List<IncotermViewModel> Incoterm { get; set; } = new List<IncotermViewModel>();
        public List<MovementTypeViewModel> Movement { get; set; } = new List<MovementTypeViewModel>();
        public List<PackageTypeViewModel> PackageType { get; set; } = new List<PackageTypeViewModel>();
        public List<UnitViewModel> Unit { get; set; } = new List<UnitViewModel>();
        public List<CurrencyViewModel> Currency { get; set; } = new List<CurrencyViewModel>();
        public List<CountryViewModel> Country { get; set; } = new List<CountryViewModel>();
        public List<CityViewModel> City { get; set; } = new List<CityViewModel>();
    }


}