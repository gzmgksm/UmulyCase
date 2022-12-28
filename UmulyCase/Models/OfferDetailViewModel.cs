namespace UmulyCase.Models
{
    public class OfferDetailViewModel
    {
        
        public long Id { get; set; } 
        public int OfferId { get; set; }
        //public int ModeId { get; set; } 
        //public int MovementTypeId { get; set; } 
        //public int IncotermId { get; set; } 
        //public int PackageTypeId { get; set; }  
        //public int UnitId { get; set; } 
        //public int CurrencyId { get; set; } 
        //public int CountryId { get; set; } 
        //public int CityId { get; set; } 

        public ModeViewModel Mode { get; set; } = new ModeViewModel();
        public IncotermViewModel Incoterm { get; set; } = new IncotermViewModel(); 
        public MovementTypeViewModel Movement { get; set; } = new MovementTypeViewModel();
        public PackageTypeViewModel PackageType { get; set; } = new PackageTypeViewModel();
        public UnitViewModel Unit { get; set; } = new UnitViewModel();
        public CurrencyViewModel Currency { get; set; } = new CurrencyViewModel();
        public CountryViewModel Country { get; set; } = new CountryViewModel();
        public CityViewModel City { get; set; }= new CityViewModel();


    }
}
