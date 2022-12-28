namespace UmulyCase.Models
{
    public class CountryViewModel
    {
        public int CountryId { get; set; }
        public string CountryName { get; set; }=String.Empty;

        public virtual List<CityViewModel> Cities { get; set; }=new List<CityViewModel>();
        //public CountryViewModel()
        //{
        //  //Name = string.Empty;
        //}
        
    }
}
