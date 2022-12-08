namespace UmulyCase.Models
{
    public class OfferViewModel
    {
        public int? Id { get; set; } 
        public string? Description { get; set; }
        public string? UserName { get; set; }
        public DateTime? OfferDate { get; set; }

        public virtual List<OfferDetailViewModel>? Details { get; set; }
        public OfferViewModel()
        {
            Description = string.Empty;
            UserName = string.Empty;    
            Details = new List<OfferDetailViewModel>();

        }
    }
}
